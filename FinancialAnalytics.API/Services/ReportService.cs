using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Services;

public class ReportService
{
    private readonly FinancialDbContext _context;
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        FinancialDbContext context,
        AnalyticsService analyticsService,
        ILogger<ReportService> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<Report> GenerateRevenueReport(DateTime startDate, DateTime endDate)
    {
        var analytics = await _analyticsService.GetRevenueAnalytics(startDate, endDate);
        var byLocation = await _analyticsService.GetRevenueByLocation(startDate, endDate);

        var reportContent = new
        {
            ReportType = "Análisis de Ingresos",
            Period = new { StartDate = startDate, EndDate = endDate },
            Summary = analytics,
            ByLocation = byLocation,
            GeneratedAt = DateTime.Now
        };

        var report = new Report
        {
            Title = $"Informe de Ingresos - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}",
            ReportType = "Ingresos",
            GeneratedDate = DateTime.Now,
            StartDate = startDate,
            EndDate = endDate,
            Content = JsonSerializer.Serialize(reportContent, new JsonSerializerOptions { WriteIndented = true }),
            GeneratedBy = "Sistema",
            Status = "Generado"
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Informe de ingresos generado: {report.Id}");
        return report;
    }

    public async Task<Report> GenerateStudentReport(DateTime startDate, DateTime endDate)
    {
        var analytics = await _analyticsService.GetStudentAnalytics();

        var reportContent = new
        {
            ReportType = "Análisis de Rendimiento Estudiantil",
            Period = new { StartDate = startDate, EndDate = endDate },
            Analytics = analytics,
            GeneratedAt = DateTime.Now
        };

        var report = new Report
        {
            Title = $"Informe de Rendimiento Estudiantil - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}",
            ReportType = "Estudiantes",
            GeneratedDate = DateTime.Now,
            StartDate = startDate,
            EndDate = endDate,
            Content = JsonSerializer.Serialize(reportContent, new JsonSerializerOptions { WriteIndented = true }),
            GeneratedBy = "Sistema",
            Status = "Generado"
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Informe de estudiantes generado: {report.Id}");
        return report;
    }

    public async Task<Report> GenerateRoomUsageReport(DateTime startDate, DateTime endDate)
    {
        var analytics = await _analyticsService.GetRoomUsageAnalytics();

        var reportContent = new
        {
            ReportType = "Análisis de Uso de Salas",
            Period = new { StartDate = startDate, EndDate = endDate },
            Analytics = analytics,
            GeneratedAt = DateTime.Now
        };

        var report = new Report
        {
            Title = $"Informe de Uso de Salas - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}",
            ReportType = "Salas",
            GeneratedDate = DateTime.Now,
            StartDate = startDate,
            EndDate = endDate,
            Content = JsonSerializer.Serialize(reportContent, new JsonSerializerOptions { WriteIndented = true }),
            GeneratedBy = "Sistema",
            Status = "Generado"
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Informe de uso de salas generado: {report.Id}");
        return report;
    }

    public async Task<Report> GenerateCustomerReport(DateTime startDate, DateTime endDate)
    {
        var segments = await _analyticsService.GetCustomerSegments();

        var reportContent = new
        {
            ReportType = "Análisis de Segmentación de Clientes",
            Period = new { StartDate = startDate, EndDate = endDate },
            Segmentation = segments,
            GeneratedAt = DateTime.Now
        };

        var report = new Report
        {
            Title = $"Informe de Análisis de Clientes - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}",
            ReportType = "Clientes",
            GeneratedDate = DateTime.Now,
            StartDate = startDate,
            EndDate = endDate,
            Content = JsonSerializer.Serialize(reportContent, new JsonSerializerOptions { WriteIndented = true }),
            GeneratedBy = "Sistema",
            Status = "Generado"
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Informe de clientes generado: {report.Id}");
        return report;
    }

    public async Task<List<Report>> GetAllReports()
    {
        return await _context.Reports
            .OrderByDescending(r => r.GeneratedDate)
            .ToListAsync();
    }

    public async Task<Report?> GetReportById(int id)
    {
        return await _context.Reports.FindAsync(id);
    }
}
