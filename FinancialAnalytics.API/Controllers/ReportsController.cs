using Microsoft.AspNetCore.Mvc;
using FinancialAnalytics.API.Services;

namespace FinancialAnalytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Get all reports
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllReports()
    {
        try
        {
            var reports = await _reportService.GetAllReports();
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get report by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReport(int id)
    {
        try
        {
            var report = await _reportService.GetReportById(id);
            if (report == null)
            {
                return NotFound(new { error = "Report not found" });
            }
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate and download revenue report as Excel file
    /// </summary>
    [HttpPost("generate/revenue")]
    public async Task<IActionResult> GenerateRevenueReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.Now.AddMonths(-6);
            endDate ??= DateTime.Now;

            var excelService = HttpContext.RequestServices.GetRequiredService<ExcelReportService>();
            var excelBytes = await excelService.GenerateRevenueReport(startDate, endDate);

            var fileName = $"Reporte_Ingresos_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate student performance report
    /// </summary>
    [HttpPost("generate/students")]
    public async Task<IActionResult> GenerateStudentReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var report = await _reportService.GenerateStudentReport(startDate.Value, endDate.Value);
            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating student report");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate room usage report
    /// </summary>
    [HttpPost("generate/rooms")]
    public async Task<IActionResult> GenerateRoomReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var report = await _reportService.GenerateRoomUsageReport(startDate.Value, endDate.Value);
            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating room report");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate customer analysis report
    /// </summary>
    [HttpPost("generate/customers")]
    public async Task<IActionResult> GenerateCustomerReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var report = await _reportService.GenerateCustomerReport(startDate.Value, endDate.Value);
            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer report");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
