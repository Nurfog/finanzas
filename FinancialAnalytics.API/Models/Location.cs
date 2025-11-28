namespace FinancialAnalytics.API.Models;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime OpeningDate { get; set; }
    
    // Navigation properties
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
