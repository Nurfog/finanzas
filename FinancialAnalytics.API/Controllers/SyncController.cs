using Microsoft.AspNetCore.Mvc;
using FinancialAnalytics.API.Services;
using FinancialAnalytics.API.Models;
using FinancialAnalytics.API.Models.Legacy;
using FinancialAnalytics.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialAnalytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncController> _logger;
    
    public SyncController(
        IServiceScopeFactory scopeFactory,
        ILogger<SyncController> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    [HttpPost("trigger")]
    public IActionResult TriggerSync()
    {
        var status = SyncStatus.Instance;
        
        if (status.IsRunning)
        {
            return BadRequest(new { message = "Ya hay una sincronización en progreso" });
        }
        
        try
        {
            _logger.LogInformation("Sincronización manual iniciada por usuario");
            
            // Run sync in background with proper scope management
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<LegacyDataSyncService>();
                    await syncService.SyncDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en sincronización manual");
                }
            });
            
            return Ok(new { message = "Sincronización iniciada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar sincronización manual");
            return StatusCode(500, new { message = "Error al iniciar sincronización" });
        }
    }
    
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = SyncStatus.Instance;
        
        return Ok(new
        {
            isRunning = status.IsRunning,
            lastSyncDate = status.LastSyncDate,
            currentSyncStarted = status.CurrentSyncStarted,
            currentStep = status.CurrentStep,
            progress = status.Progress,
            message = status.Message,
            hasError = status.HasError,
            errorMessage = status.ErrorMessage
        });
    }

    [HttpGet("inspect")]
    public async Task<IActionResult> InspectSchema([FromServices] FinancialDbContext context)
    {
        try 
        {
            var result = new List<Dictionary<string, object>>();
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM vw_legacy_diagnostico LIMIT 1";
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("update-schema")]
    public async Task<IActionResult> UpdateSchema([FromServices] FinancialDbContext context)
    {
        var errors = new List<string>();
        try 
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();

            // 1. Make Transactions.LocationId nullable
            try {
                cmd.CommandText = "ALTER TABLE Transactions MODIFY COLUMN LocationId int NULL;";
                await cmd.ExecuteNonQueryAsync(); 
            } catch (Exception ex) { errors.Add($"Transactions LocationId: {ex.Message}"); }

            // 1b. Change Transactions.Amount to int (for CLP)
            try {
                cmd.CommandText = "ALTER TABLE Transactions MODIFY COLUMN Amount int NOT NULL;";
                await cmd.ExecuteNonQueryAsync(); 
            } catch (Exception ex) { errors.Add($"Transactions Amount: {ex.Message}"); }

            // 2. Add Status to Students
            try {
                // Try without IF NOT EXISTS first to see real error if any
                cmd.CommandText = "ALTER TABLE Students ADD COLUMN Status longtext NULL;";
                await cmd.ExecuteNonQueryAsync(); 
            } catch (Exception ex) { errors.Add($"Students Status: {ex.Message}"); }

            // 3. Add AssessmentType to StudentProgress
            try {
                cmd.CommandText = "ALTER TABLE StudentProgress ADD COLUMN AssessmentType longtext NULL;";
                await cmd.ExecuteNonQueryAsync(); 
            } catch (Exception ex) { errors.Add($"StudentProgress AssessmentType: {ex.Message}"); }

            // 4. Update existing null values
            try {
                cmd.CommandText = "UPDATE Students SET Status = 'Active' WHERE Status IS NULL;";
                await cmd.ExecuteNonQueryAsync(); 
            } catch (Exception ex) { errors.Add($"Update Students: {ex.Message}"); }

            try {
                cmd.CommandText = "UPDATE StudentProgress SET AssessmentType = 'Exam' WHERE AssessmentType IS NULL;";
                await cmd.ExecuteNonQueryAsync(); 
            } catch (Exception ex) { errors.Add($"Update StudentProgress: {ex.Message}"); }

            return Ok(new { message = "Schema update attempted", errors });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("clear-database")]
    public async Task<IActionResult> ClearDatabase([FromServices] FinancialDbContext context)
    {
        try 
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();

            // Disable foreign key checks
            cmd.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";
            await cmd.ExecuteNonQueryAsync();

            // Clear all tables
            var tables = new[] { "StudentProgress", "Students", "Transactions", "RoomUsages", "Rooms", "Locations", "Customers", "Reports" };
            foreach (var table in tables)
            {
                cmd.CommandText = $"TRUNCATE TABLE {table};";
                await cmd.ExecuteNonQueryAsync();
            }

            // Re-enable foreign key checks
            cmd.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";
            await cmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Database cleared successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
