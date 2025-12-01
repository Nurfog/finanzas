namespace FinancialAnalytics.API.Models;

public class StudentProgress
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime AssessmentDate { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = "Exam"; // Quiz, Exam, Project, Homework
    public decimal Score { get; set; } // 0-100
    public int AttendanceRate { get; set; } // Percentage
    public string PerformanceLevel { get; set; } = string.Empty; // Excellent, Good, Average, Poor
    public string Notes { get; set; } = string.Empty;
    
    // Navigation properties
    public Student Student { get; set; } = null!;
}
