using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialAnalytics.API.Models;

public class DiagnosticResult
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; }

    public DateTime AssessmentDate { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; }
    
    public string Type { get; set; } // e.g., "Adults", "Kids"
    
    public string ResultData { get; set; } // JSON blob with detailed answers
}
