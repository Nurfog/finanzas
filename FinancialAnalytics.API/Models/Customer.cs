namespace FinancialAnalytics.API.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public string CustomerType { get; set; } = "Regular"; // Regular, Premium, VIP
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
