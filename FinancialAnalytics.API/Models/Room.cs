namespace FinancialAnalytics.API.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string RoomType { get; set; } = string.Empty; // Classroom, Lab, Conference, etc.
    public int LocationId { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Location Location { get; set; } = null!;
    public ICollection<RoomUsage> RoomUsages { get; set; } = new List<RoomUsage>();
}
