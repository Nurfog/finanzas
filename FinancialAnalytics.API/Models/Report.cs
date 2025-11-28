namespace FinancialAnalytics.API.Models;

public class Report
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty; // Revenue, Student, Room, Customer
    public DateTime GeneratedDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Content { get; set; } = string.Empty; // JSON or HTML content
    public string GeneratedBy { get; set; } = "System";
    public string Status { get; set; } = "Generated"; // Generated, Archived
}
