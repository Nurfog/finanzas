namespace FinancialAnalytics.API.Models;

public class Transaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int LocationId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Payment, Refund, Fee, etc.
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Transfer, etc.
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed"; // Completed, Pending, Cancelled
    
    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
