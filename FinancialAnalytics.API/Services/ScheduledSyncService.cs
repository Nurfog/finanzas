using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Services;

public class ScheduledSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledSyncService> _logger;
    private Timer? _timer;
    
    public ScheduledSyncService(
        IServiceProvider serviceProvider,
        ILogger<ScheduledSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled Sync Service iniciado");
        
        // Calculate time until next 2 AM
        var now = DateTime.Now;
        var nextRun = now.Date.AddDays(1).AddHours(2); // Tomorrow at 2 AM
        
        if (now.Hour < 2)
        {
            // If it's before 2 AM today, run today at 2 AM
            nextRun = now.Date.AddHours(2);
        }
        
        var timeUntilNextRun = nextRun - now;
        _logger.LogInformation($"Próxima sincronización automática: {nextRun:yyyy-MM-dd HH:mm:ss}");
        
        // Set up timer to run daily at 2 AM
        _timer = new Timer(
            DoWork,
            null,
            timeUntilNextRun,
            TimeSpan.FromHours(24)); // Repeat every 24 hours
        
        return Task.CompletedTask;
    }
    
    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("Ejecutando sincronización automática programada...");
            
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<LegacyDataSyncService>();
            
            await syncService.SyncDataAsync();
            
            _logger.LogInformation("Sincronización automática completada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sincronización automática programada");
        }
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled Sync Service detenido");
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        return base.StopAsync(cancellationToken);
    }
    
    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
