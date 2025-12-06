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

        var totalRevenue = transactions.Sum(t => (long)t.Amount);
        var averageTransaction = transactions.Any() ? transactions.Average(t => t.Amount) : 0;
        var transactionCount = transactions.Count;

        var byMonth = transactions
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(t => (long)t.Amount),
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
                Revenue = g.Sum(t => (long)t.Amount),
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
                
                // Proyectar un crecimiento conservador del 2% mensual para los inputs
                // Esto permite que el modelo reaccione a la tendencia en lugar de recibir inputs estáticos
                float projectedCustomers = (float)(avgCustomers * Math.Pow(1.02, i));
                float projectedTransactions = (float)(avgTransactions * Math.Pow(1.02, i));

                // Ajuste estacional simple para la proyección de inputs
                if (futureDate.Month == 3 || futureDate.Month == 12) projectedTransactions *= 1.1f; // Picos en Marzo/Diciembre
                if (futureDate.Month == 1 || futureDate.Month == 2) projectedTransactions *= 0.8f;  // Baja en verano

                var input = new RevenueData
                {
                    Month = futureDate.Month,
                    LocationId = locationId,
                    CustomerCount = projectedCustomers,
                    TransactionCount = projectedTransactions
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
    public async Task<object> GetCustomerSegments(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.Now.AddMonths(-6);
            endDate ??= DateTime.Now;

            var model = _mlService.LoadModel("CustomerSegmentation", out var schema);
            if (model == null)
            {
                return new { Error = "El modelo de segmentación de clientes aún no ha sido entrenado" };
            }

            var predictionEngine = _mlService.Context.Model.CreatePredictionEngine<CustomerData, CustomerSegmentation>(model);

            var customers = await _context.Customers
                .Include(c => c.Transactions)
                .ToListAsync();

            var segments = customers.Select(c =>
            {
                // Filter transactions by date range
                var relevantTransactions = c.Transactions
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                    .ToList();

                // If no transactions in range, we might still want to classify them based on history? 
                // Or maybe treat them as "Inactive" for this period?
                // For now, let's calculate stats based on the period. If 0 transactions, stats are 0.
                
                var customerData = new CustomerData
                {
                    CustomerId = c.Id,
                    TotalSpent = (float)relevantTransactions.Sum(t => (long)t.Amount),
                    TransactionFrequency = relevantTransactions.Count,
                    DaysSinceRegistration = (float)(DateTime.Now - c.RegistrationDate).TotalDays, // This remains total history
                    AverageTransactionValue = relevantTransactions.Any() ? (float)relevantTransactions.Average(t => t.Amount) : 0
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
    public async Task<object> GetRoomUsageAnalytics(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddMonths(-3);
        endDate ??= DateTime.Now;

        var roomUsages = await _context.RoomUsages
            .Include(r => r.Room)
            .ThenInclude(room => room.Location)
            .Where(r => r.StartTime >= startDate && r.EndTime <= endDate)
            .Where(r => !r.Room.Location.Name.Contains("ONLINE")) // Maintain consistency with other endpoints
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
    public async Task<object> GetStudentAnalytics(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddMonths(-6);
        endDate ??= DateTime.Now;

        // We want students who have activity in this period
        // Logic: A student is active if their course (assumed 6 months duration) overlaps with the selected range
        // AND they are marked as Active in the system.
        var allActiveStudents = await _context.Students
            .Include(s => s.ProgressRecords)
            .Where(s => s.IsActive)
            .ToListAsync();

        var studentStats = allActiveStudents.Select(s =>
        {
            // Assume course duration is 6 months from enrollment
            var courseStart = s.EnrollmentDate;
            var courseEnd = s.EnrollmentDate.AddMonths(6);

            // Check for overlap: (StartA <= EndB) and (EndA >= StartB)
            bool isCourseActiveInRange = courseStart <= endDate && courseEnd >= startDate;

            if (!isCourseActiveInRange) return null;

            // For stats, we still only look at progress records within the range (or all if we want cumulative?)
            // User likely wants to see performance *during* that time, or latest status.
            // Let's show relevant records for the period, but if none, still show the student as active (with 0 score).
            var relevantRecords = s.ProgressRecords
                .Where(p => p.AssessmentDate >= startDate && p.AssessmentDate <= endDate)
                .ToList();

            var latestProgress = relevantRecords.OrderByDescending(p => p.AssessmentDate).FirstOrDefault() 
                                 ?? s.ProgressRecords.OrderByDescending(p => p.AssessmentDate).FirstOrDefault(); // Fallback to latest overall if none in range?
            
            // Actually, for "Average Score" in this period, we should only use relevant records.
            // If no records in period, average is 0 or N/A.
            var averageScore = relevantRecords.Any() ? relevantRecords.Average(p => p.Score) : 0;

            return new
            {
                StudentId = s.Id,
                StudentName = s.Name,
                Program = s.Program,
                AverageScore = Math.Round(averageScore, 2),
                LatestScore = latestProgress?.Score ?? 0,
                LatestPerformance = latestProgress?.PerformanceLevel ?? "N/A",
                AssessmentCount = relevantRecords.Count
            };
        })
        .Where(s => s != null) // Filter out students whose course wasn't active
        .ToList();

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
            TotalStudents = studentStats.Count
        };
    }

    // Advanced Room Analytics
    public async Task<object> GetRoomUtilizationByLocation(int? locationId, DateTime startDate, DateTime endDate)
    {
        var query = _context.RoomUsages
            .Include(r => r.Room)
            .ThenInclude(room => room.Location)
            .Where(r => r.StartTime >= startDate && r.StartTime <= endDate);

        // Exclude Online rooms for physical room analysis
        query = query.Where(r => !r.Room.Location.Name.Contains("ONLINE"));

        if (locationId.HasValue)
        {
            query = query.Where(r => r.Room.LocationId == locationId.Value);
        }

        var roomUsages = await query.ToListAsync();
        
        // Calculate total available hours in the period (8:30 to 21:15 = 12.75 hours/day)
        var totalDays = (endDate - startDate).TotalDays;
        var availableHoursPerRoom = totalDays * 12.75; 

        var utilizationByRoom = roomUsages
            .GroupBy(r => new { r.RoomId, RoomName = r.Room.Name, LocationName = r.Room.Location.Name, Capacity = r.Room.Capacity })
            .Select(g => {
                var totalHoursUsed = g.Sum(x => (x.EndTime - x.StartTime).TotalHours);
                var capacityUtilization = g.Average(x => (double)x.UtilizationRate); // How full is it when used
                var timeUtilization = totalHoursUsed / availableHoursPerRoom; // How often is it used
                
                return new
                {
                    RoomId = g.Key.RoomId,
                    RoomName = g.Key.RoomName,
                    LocationName = g.Key.LocationName,
                    Capacity = g.Key.Capacity,
                    CapacityUtilization = capacityUtilization,
                    TimeUtilization = timeUtilization,
                    CombinedEfficiency = capacityUtilization * timeUtilization,
                    TotalSessions = g.Count(),
                    TotalAttendees = g.Sum(x => x.AttendeeCount),
                    AverageAttendees = g.Average(x => x.AttendeeCount),
                    TotalHours = totalHoursUsed
                };
            })
            .OrderByDescending(x => x.CombinedEfficiency)
            .ToList();

        var utilizationByLocation = roomUsages
            .GroupBy(r => new { LocationId = r.Room.LocationId, LocationName = r.Room.Location.Name })
            .Select(g => {
                var roomCount = g.Select(x => x.RoomId).Distinct().Count();
                var totalHoursUsed = g.Sum(x => (x.EndTime - x.StartTime).TotalHours);
                var totalAvailableHours = roomCount * availableHoursPerRoom;

                return new
                {
                    LocationId = g.Key.LocationId,
                    LocationName = g.Key.LocationName,
                    CapacityUtilization = g.Average(x => (double)x.UtilizationRate),
                    TimeUtilization = totalAvailableHours > 0 ? totalHoursUsed / totalAvailableHours : 0,
                    TotalSessions = g.Count(),
                    RoomCount = roomCount
                };
            })
            .OrderByDescending(x => x.TimeUtilization)
            .ToList();

        // Calculate Overall Time Utilization
        var totalHoursAllRooms = roomUsages.Sum(x => (x.EndTime - x.StartTime).TotalHours);
        var totalRoomsCount = roomUsages.Select(x => x.RoomId).Distinct().Count();
        var totalGlobalAvailableHours = totalRoomsCount * availableHoursPerRoom;
        var overallTimeUtilization = totalGlobalAvailableHours > 0 ? totalHoursAllRooms / totalGlobalAvailableHours : 0;

        return new
        {
            ByRoom = utilizationByRoom,
            ByLocation = utilizationByLocation,
            OverallCapacityUtilization = roomUsages.Any() ? roomUsages.Average(r => (double)r.UtilizationRate) : 0,
            OverallTimeUtilization = overallTimeUtilization,
            DateRange = new { StartDate = startDate, EndDate = endDate }
        };
    }

    public async Task<object> GetRoomUsagePatternsByDayOfWeek(int? locationId)
    {
        var query = _context.RoomUsages
            .Include(r => r.Room)
            .ThenInclude(room => room.Location)
            .Where(r => !r.Room.Location.Name.Contains("ONLINE")) // Exclude Online rooms
            .AsQueryable();

        if (locationId.HasValue)
        {
            query = query.Where(r => r.Room.LocationId == locationId.Value);
        }

        var roomUsages = await query.ToListAsync();

        var patternsByDay = roomUsages
            .GroupBy(r => r.StartTime.DayOfWeek)
            .Select(g => new
            {
                DayOfWeek = g.Key.ToString(),
                DayNumber = (int)g.Key,
                SessionCount = g.Count(),
                AverageUtilization = g.Average(x => (double)x.UtilizationRate),
                AverageAttendees = g.Average(x => x.AttendeeCount),
                TotalAttendees = g.Sum(x => x.AttendeeCount),
                PeakHour = g.GroupBy(x => x.StartTime.Hour)
                           .OrderByDescending(h => h.Count())
                           .Select(h => h.Key)
                           .FirstOrDefault()
            })
            .OrderBy(x => x.DayNumber)
            .ToList();

        var patternsByHour = roomUsages
            .GroupBy(r => r.StartTime.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                SessionCount = g.Count(),
                AverageUtilization = g.Average(x => (double)x.UtilizationRate),
                AverageAttendees = g.Average(x => x.AttendeeCount)
            })
            .OrderBy(x => x.Hour)
            .ToList();

        // Identify peak and low days
        var peakDay = patternsByDay.OrderByDescending(x => x.SessionCount).FirstOrDefault();
        var lowDay = patternsByDay.OrderBy(x => x.SessionCount).FirstOrDefault();

        return new
        {
            ByDayOfWeek = patternsByDay,
            ByHour = patternsByHour,
            PeakDay = peakDay?.DayOfWeek,
            LowDay = lowDay?.DayOfWeek,
            TotalSessions = roomUsages.Count
        };
    }

    public async Task<object> GetUnderutilizedRooms(decimal threshold = 0.5m)
    {
        var roomUsages = await _context.RoomUsages
            .Include(r => r.Room)
            .ThenInclude(room => room.Location)
            .Where(r => !r.Room.Location.Name.Contains("ONLINE")) // Exclude Online rooms
            .ToListAsync();

        // Calculate available hours (assuming 30 days analysis for this specific endpoint if not specified, 
        // but since we don't have dates here, we'll use the range found in data or default to 12h/day)
        // For accurate TimeUtilization we need the date range. 
        // Let's assume the data passed is within a relevant range or calculate based on min/max date in data.
        var minDate = roomUsages.Any() ? roomUsages.Min(r => r.StartTime) : DateTime.Now;
        var maxDate = roomUsages.Any() ? roomUsages.Max(r => r.EndTime) : DateTime.Now;
        var totalDays = (maxDate - minDate).TotalDays;
        if (totalDays < 1) totalDays = 1;
        var availableHoursPerRoom = totalDays * 12.75; // 8:30 to 21:15

        var roomStats = roomUsages
            .GroupBy(r => new { r.RoomId, RoomName = r.Room.Name, LocationName = r.Room.Location.Name, Capacity = r.Room.Capacity })
            .Select(g => {
                var totalHoursUsed = g.Sum(x => (x.EndTime - x.StartTime).TotalHours);
                var capacityUtilization = g.Average(x => (double)x.UtilizationRate);
                var timeUtilization = totalHoursUsed / availableHoursPerRoom;

                return new
                {
                    RoomId = g.Key.RoomId,
                    RoomName = g.Key.RoomName,
                    LocationName = g.Key.LocationName,
                    Capacity = g.Key.Capacity,
                    CapacityUtilization = capacityUtilization,
                    TimeUtilization = timeUtilization,
                    CombinedEfficiency = capacityUtilization * timeUtilization,
                    TotalSessions = g.Count(),
                    AverageAttendees = g.Average(x => x.AttendeeCount),
                    TotalHours = totalHoursUsed,
                    WastedCapacity = g.Sum(x => x.Room.Capacity - x.AttendeeCount)
                };
            })
            .Where(x => x.TimeUtilization < (double)threshold) // Use TimeUtilization for "Underutilized"
            .OrderBy(x => x.TimeUtilization)
            .ToList();

        // Calculate opportunity cost (hours available but underutilized)
        var totalWastedHours = roomStats.Sum(x => (availableHoursPerRoom - x.TotalHours));

        return new
        {
            UnderutilizedRooms = roomStats,
            Threshold = threshold,
            TotalRoomsUnderutilized = roomStats.Count,
            TotalWastedHours = totalWastedHours,
            TotalWastedCapacity = roomStats.Sum(x => x.WastedCapacity)
        };
    }

    public async Task<object> GetRoomOptimizationSuggestions()
    {
        var roomUsages = await _context.RoomUsages
            .Include(r => r.Room)
            .ThenInclude(room => room.Location)
            .Where(r => !r.Room.Location.Name.Contains("ONLINE")) // Exclude Online rooms
            .ToListAsync();

        var roomStats = roomUsages
            .GroupBy(r => new { r.RoomId, RoomName = r.Room.Name, LocationName = r.Room.Location.Name, Capacity = r.Room.Capacity })
            .Select(g => new
            {
                RoomId = g.Key.RoomId,
                RoomName = g.Key.RoomName,
                LocationName = g.Key.LocationName,
                Capacity = g.Key.Capacity,
                AverageUtilization = g.Average(x => (double)x.UtilizationRate),
                AverageAttendees = g.Average(x => x.AttendeeCount),
                TotalSessions = g.Count()
            })
            .ToList();

        var suggestions = new List<object>();

        // Suggestion 1: Rooms with capacity much larger than average attendance
        var oversizedRooms = roomStats
            .Where(r => r.Capacity > r.AverageAttendees * 2)
            .Select(r => new
            {
                Type = "Downsize",
                RoomName = r.RoomName,
                LocationName = r.LocationName,
                CurrentCapacity = r.Capacity,
                AverageAttendees = Math.Round(r.AverageAttendees, 0),
                Suggestion = $"Considerar reasignar cursos a sala más pequeña. Capacidad actual ({r.Capacity}) es el doble del promedio de asistentes ({Math.Round(r.AverageAttendees, 0)}).",
                PotentialSavings = $"{Math.Round((1 - r.AverageAttendees / r.Capacity) * 100, 0)}% de espacio liberado"
            });

        suggestions.AddRange(oversizedRooms);

        // Suggestion 2: Highly utilized rooms that might need expansion
        var overcrowdedRooms = roomStats
            .Where(r => r.AverageUtilization > 0.9)
            .Select(r => new
            {
                Type = "Expand",
                RoomName = r.RoomName,
                LocationName = r.LocationName,
                CurrentCapacity = r.Capacity,
                AverageAttendees = Math.Round(r.AverageAttendees, 0),
                Utilization = Math.Round(r.AverageUtilization * 100, 0),
                Suggestion = $"Sala con alta demanda ({Math.Round(r.AverageUtilization * 100, 0)}% utilización). Considerar asignar sala más grande o dividir cursos.",
                PotentialImpact = "Mejorar experiencia de estudiantes y permitir más inscripciones"
            });

        suggestions.AddRange(overcrowdedRooms);

        // Suggestion 3: Underutilized rooms
        var underutilizedRooms = roomStats
            .Where(r => r.AverageUtilization < 0.5)
            .Select(r => new
            {
                Type = "Consolidate",
                RoomName = r.RoomName,
                LocationName = r.LocationName,
                Utilization = Math.Round(r.AverageUtilization * 100, 0),
                TotalSessions = r.TotalSessions,
                Suggestion = $"Sala subutilizada ({Math.Round(r.AverageUtilization * 100, 0)}% utilización). Considerar consolidar con otras salas o aumentar oferta de cursos.",
                PotentialSavings = "Reducir costos operativos de mantenimiento"
            });

        suggestions.AddRange(underutilizedRooms);

        return new
        {
            Suggestions = suggestions,
            TotalSuggestions = suggestions.Count,
            Summary = new
            {
                RoomsToDownsize = oversizedRooms.Count(),
                RoomsToExpand = overcrowdedRooms.Count(),
                RoomsToConsolidate = underutilizedRooms.Count()
            }
        };
    }
}
