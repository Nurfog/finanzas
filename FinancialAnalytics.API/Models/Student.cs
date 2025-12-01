namespace FinancialAnalytics.API.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string Program { get; set; } = string.Empty; // Course or program name
    public string Status { get; set; } = "Active"; // Active, Graduated, Dropped, Suspended
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
}
