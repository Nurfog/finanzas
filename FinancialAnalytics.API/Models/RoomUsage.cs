namespace FinancialAnalytics.API.Models;

public class RoomUsage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int AttendeeCount { get; set; }
    public string Purpose { get; set; } = string.Empty; // Class, Meeting, Event, etc.
    public decimal UtilizationRate { get; set; } // Percentage of capacity used
    
    // Navigation properties
    public Room Room { get; set; } = null!;
}
