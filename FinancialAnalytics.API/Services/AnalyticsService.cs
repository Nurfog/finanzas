using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Services;

public class AnalyticsService
{
    private readonly FinancialDbContext _context;
    private readonly MLModelService _mlService;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        FinancialDbContext context,
        MLModelService mlService,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _mlService = mlService;
        _logger = logger;
    }

    // Análisis de Ingresos
    public async Task<object> GetRevenueAnalytics(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddMonths(-6);
        endDate ??= DateTime.Now;

        var transactions = await _context.Transactions
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.Status == "Completed")
            .ToListAsync();

        var totalRevenue = transactions.Sum(t => t.Amount);
        var averageTransaction = transactions.Any() ? transactions.Average(t => t.Amount) : 0;
        var transactionCount = transactions.Count;

        var byMonth = transactions
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var byPaymentMethod = transactions
            .GroupBy(t => t.PaymentMethod)
            .Select(g => new
            {
                PaymentMethod = g.Key,
                Revenue = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToList();

        return new
        {
            TotalRevenue = totalRevenue,
            AverageTransaction = averageTransaction,
            TransactionCount = transactionCount,
            ByMonth = byMonth,
            ByPaymentMethod = byPaymentMethod
        };
    }

    public async Task<object> GetRevenueByLocation(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddMonths(-6);
        endDate ??= DateTime.Now;

        var revenueByLocation = await _context.Transactions
            .Include(t => t.Location)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.Status == "Completed")
            .GroupBy(t => new { t.LocationId, t.Location.Name })
            .Select(g => new
            {
                LocationId = g.Key.LocationId,
                LocationName = g.Key.Name,
                Revenue = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageTransaction = g.Average(t => t.Amount)
            })
            .ToListAsync();

        return revenueByLocation;
    }

    public async Task<object> PredictRevenue(int locationId, int monthsAhead = 3)
    {
        try
        {
            var model = _mlService.LoadModel("RevenuePredictor", out var schema);
            if (model == null)
            {
                return new { Error = "El modelo de predicción de ingresos aún no ha sido entrenado" };
            }

            var predictionEngine = _mlService.Context.Model.CreatePredictionEngine<RevenueData, RevenuePrediction>(model);

            var predictions = new List<object>();
            var currentDate = DateTime.Now;

            // Obtener datos históricos para contexto
            var recentData = await _context.Transactions
                .Where(t => t.LocationId == locationId && t.Status == "Completed")
                .GroupBy(t => new { t.TransactionDate.Month, t.TransactionDate.Year })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    CustomerCount = g.Select(t => t.CustomerId).Distinct().Count(),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                .Take(3)
                .ToListAsync();

            var avgCustomers = recentData.Any() ? recentData.Average(x => x.CustomerCount) : 10;
            var avgTransactions = recentData.Any() ? recentData.Average(x => x.TransactionCount) : 20;

            for (int i = 1; i <= monthsAhead; i++)
            {
                var futureDate = currentDate.AddMonths(i);
                var input = new RevenueData
                {
                    Month = futureDate.Month,
                    LocationId = locationId,
                    CustomerCount = (float)avgCustomers,
                    TransactionCount = (float)avgTransactions
                };

                var prediction = predictionEngine.Predict(input);
                predictions.Add(new
                {
                    Month = futureDate.Month,
                    Year = futureDate.Year,
                    PredictedRevenue = Math.Round(prediction.PredictedRevenue, 2)
                });
            }

            return new { LocationId = locationId, Predictions = predictions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error prediciendo ingresos");
            return new { Error = ex.Message };
        }
    }

    // Análisis de Clientes
    public async Task<object> GetCustomerSegments()
    {
        try
        {
            var model = _mlService.LoadModel("CustomerSegmentation", out var schema);
            if (model == null)
            {
                return new { Error = "El modelo de segmentación de clientes aún no ha sido entrenado" };
            }

            var predictionEngine = _mlService.Context.Model.CreatePredictionEngine<CustomerData, CustomerSegmentation>(model);

            var customers = await _context.Customers
                .Include(c => c.Transactions)
                .Where(c => c.IsActive)
                .ToListAsync();

            var segments = customers.Select(c =>
            {
                var customerData = new CustomerData
                {
                    CustomerId = c.Id,
                    TotalSpent = (float)c.Transactions.Sum(t => t.Amount),
                    TransactionFrequency = c.Transactions.Count,
                    DaysSinceRegistration = (float)(DateTime.Now - c.RegistrationDate).TotalDays,
                    AverageTransactionValue = c.Transactions.Any() ? (float)c.Transactions.Average(t => t.Amount) : 0
                };

                var prediction = predictionEngine.Predict(customerData);

                return new
                {
                    CustomerId = c.Id,
                    CustomerName = c.Name,
                    Segment = prediction.Segment,
                    TotalSpent = customerData.TotalSpent,
                    TransactionCount = customerData.TransactionFrequency
                };
            }).ToList();

            var segmentSummary = segments
                .GroupBy(s => s.Segment)
                .Select(g => new
                {
                    Segment = g.Key,
                    CustomerCount = g.Count(),
                    AverageSpent = g.Average(x => x.TotalSpent),
                    TotalRevenue = g.Sum(x => x.TotalSpent)
                })
                .ToList();

            return new { Segments = segments, Summary = segmentSummary };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo segmentos de clientes");
            return new { Error = ex.Message };
        }
    }

    // Análisis de Salas
    public async Task<object> GetRoomUsageAnalytics()
    {
        var roomUsages = await _context.RoomUsages
            .Include(r => r.Room)
            .ThenInclude(room => room.Location)
            .ToListAsync();

        var byRoom = roomUsages
            .GroupBy(r => new { r.RoomId, RoomName = r.Room.Name, LocationName = r.Room.Location.Name })
            .Select(g => new
            {
                RoomId = g.Key.RoomId,
                RoomName = g.Key.RoomName,
                LocationName = g.Key.LocationName,
                AverageUtilization = g.Average(x => x.UtilizationRate),
                TotalSessions = g.Count(),
                TotalAttendees = g.Sum(x => x.AttendeeCount)
            })
            .OrderByDescending(x => x.AverageUtilization)
            .ToList();

        var byDayOfWeek = roomUsages
            .GroupBy(r => r.StartTime.DayOfWeek)
            .Select(g => new
            {
                DayOfWeek = g.Key.ToString(),
                AverageUtilization = g.Average(x => x.UtilizationRate),
                SessionCount = g.Count()
            })
            .ToList();

        return new
        {
            ByRoom = byRoom,
            ByDayOfWeek = byDayOfWeek,
            OverallUtilization = roomUsages.Any() ? roomUsages.Average(r => r.UtilizationRate) : 0
        };
    }

    // Análisis de Estudiantes
    public async Task<object> GetStudentAnalytics()
    {
        var students = await _context.Students
            .Include(s => s.ProgressRecords)
            .Where(s => s.IsActive)
            .ToListAsync();

        var studentStats = students.Select(s =>
        {
            var latestProgress = s.ProgressRecords.OrderByDescending(p => p.AssessmentDate).FirstOrDefault();
            var averageScore = s.ProgressRecords.Any() ? s.ProgressRecords.Average(p => p.Score) : 0;

            return new
            {
                StudentId = s.Id,
                StudentName = s.Name,
                Program = s.Program,
                AverageScore = Math.Round(averageScore, 2),
                LatestScore = latestProgress?.Score ?? 0,
                LatestPerformance = latestProgress?.PerformanceLevel ?? "N/A",
                AssessmentCount = s.ProgressRecords.Count
            };
        }).ToList();

        var byPerformance = studentStats
            .GroupBy(s => s.LatestPerformance)
            .Select(g => new
            {
                PerformanceLevel = g.Key,
                StudentCount = g.Count(),
                AverageScore = g.Average(x => x.AverageScore)
            })
            .ToList();

        return new
        {
            Students = studentStats,
            ByPerformance = byPerformance,
            TotalStudents = students.Count
        };
    }
}
