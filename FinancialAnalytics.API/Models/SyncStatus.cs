namespace FinancialAnalytics.API.Models;

public class SyncStatus
{
    public bool IsRunning { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public DateTime? CurrentSyncStarted { get; set; }
    public string CurrentStep { get; set; } = "";
    public int Progress { get; set; } // 0-100
    public string Message { get; set; } = "";
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    
    private static readonly object _lock = new object();
    private static SyncStatus? _instance;
    
    public static SyncStatus Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new SyncStatus();
                }
                return _instance;
            }
        }
    }
    
    public void StartSync()
    {
        lock (_lock)
        {
            IsRunning = true;
            CurrentSyncStarted = DateTime.Now;
            Progress = 0;
            HasError = false;
            ErrorMessage = null;
            Message = "Iniciando sincronización...";
        }
    }
    
    public void UpdateProgress(string step, int progress, string message)
    {
        lock (_lock)
        {
            CurrentStep = step;
            Progress = progress;
            Message = message;
        }
    }
    
    public void CompleteSync(bool success, string? errorMessage = null)
    {
        lock (_lock)
        {
            IsRunning = false;
            Progress = success ? 100 : Progress;
            HasError = !success;
            ErrorMessage = errorMessage;
            
            if (success)
            {
                LastSyncDate = DateTime.Now;
                Message = "Sincronización completada exitosamente";
            }
            else
            {
                Message = "Sincronización falló";
            }
            
            CurrentSyncStarted = null;
        }
    }
}
