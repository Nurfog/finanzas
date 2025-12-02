using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using FinancialAnalytics.API.Data;

namespace FinancialAnalytics.API.Services;

public class MLTrainingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MLTrainingService> _logger;
    private readonly int _trainingIntervalHours;

    public MLTrainingService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<MLTrainingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _trainingIntervalHours = configuration.GetValue<int>("MLSettings:AutoTrainInterval", 24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Entrenamiento ML iniciado");

        // Entrenamiento inicial al arrancar
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Esperar a que la app inicie completamente
        await TrainAllModels();

        // Entrenamiento periódico
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(_trainingIntervalHours), stoppingToken);
            await TrainAllModels();
        }
    }

    private async Task TrainAllModels()
    {
        _logger.LogInformation("Iniciando entrenamiento automático de modelos...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
            var mlService = scope.ServiceProvider.GetRequiredService<MLModelService>();

            await TrainRevenueModel(context, mlService);
            await TrainCustomerSegmentationModel(context, mlService);
            await TrainRoomUsageModel(context, mlService);
            await TrainStudentPerformanceModel(context, mlService);

            _logger.LogInformation("Todos los modelos fueron entrenados exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el entrenamiento de modelos");
        }
    }

    private async Task TrainRevenueModel(FinancialDbContext context, MLModelService mlService)
    {
        try
        {
            var transactions = await context.Transactions
                .Include(t => t.Location)
                .Where(t => t.Status == "Completed")
                .ToListAsync();

            if (transactions.Count < 10)
            {
                _logger.LogWarning("No hay suficientes datos de transacciones para entrenar el modelo de ingresos");
                return;
            }

            // Agrupar por mes y ubicación
            var revenueData = transactions
                .GroupBy(t => new { t.LocationId, Month = t.TransactionDate.Month, Year = t.TransactionDate.Year })
                .Select(g => new RevenueData
                {
                    Month = g.Key.Month,
                    LocationId = (float)(g.Key.LocationId ?? 0),
                    CustomerCount = g.Select(t => t.CustomerId).Distinct().Count(),
                    TransactionCount = g.Count(),
                    Revenue = (float)g.Sum(t => (long)t.Amount)
                })
                .ToList();

            var dataView = mlService.Context.Data.LoadFromEnumerable(revenueData);

            // Construir pipeline
            var pipeline = mlService.Context.Transforms.Concatenate("Features",
                    nameof(RevenueData.Month),
                    nameof(RevenueData.LocationId),
                    nameof(RevenueData.CustomerCount),
                    nameof(RevenueData.TransactionCount))
                .Append(mlService.Context.Regression.Trainers.FastTree(
                    labelColumnName: nameof(RevenueData.Revenue),
                    featureColumnName: "Features"));

            var model = pipeline.Fit(dataView);
            mlService.SaveModel(model, dataView.Schema, "RevenuePredictor");

            _logger.LogInformation("Modelo de predicción de ingresos entrenado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entrenando modelo de ingresos");
        }
    }

    private async Task TrainCustomerSegmentationModel(FinancialDbContext context, MLModelService mlService)
    {
        try
        {
            var customers = await context.Customers
                .Include(c => c.Transactions)
                .Where(c => c.IsActive)
                .ToListAsync();

            if (customers.Count < 5)
            {
                _logger.LogWarning("No hay suficientes datos de clientes para entrenar el modelo de segmentación");
                return;
            }

            var customerData = customers.Select(c => new CustomerData
            {
                CustomerId = c.Id,
                TotalSpent = (float)c.Transactions.Sum(t => (long)t.Amount),
                TransactionFrequency = c.Transactions.Count,
                DaysSinceRegistration = (float)(DateTime.Now - c.RegistrationDate).TotalDays,
                AverageTransactionValue = c.Transactions.Any() 
                    ? (float)c.Transactions.Average(t => t.Amount) 
                    : 0
            }).ToList();

            var dataView = mlService.Context.Data.LoadFromEnumerable(customerData);

            var pipeline = mlService.Context.Transforms.Concatenate("Features",
                    nameof(CustomerData.TotalSpent),
                    nameof(CustomerData.TransactionFrequency),
                    nameof(CustomerData.DaysSinceRegistration),
                    nameof(CustomerData.AverageTransactionValue))
                .Append(mlService.Context.Clustering.Trainers.KMeans(
                    featureColumnName: "Features",
                    numberOfClusters: 3));

            var model = pipeline.Fit(dataView);
            mlService.SaveModel(model, dataView.Schema, "CustomerSegmentation");

            _logger.LogInformation("Modelo de segmentación de clientes entrenado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entrenando modelo de segmentación de clientes");
        }
    }

    private async Task TrainRoomUsageModel(FinancialDbContext context, MLModelService mlService)
    {
        try
        {
            var roomUsages = await context.RoomUsages
                .Include(r => r.Room)
                .ToListAsync();

            if (roomUsages.Count < 10)
            {
                _logger.LogWarning("No hay suficientes datos de uso de salas para entrenar el modelo");
                return;
            }

            var usageData = roomUsages.Select(r => new RoomUsageData
            {
                RoomId = r.RoomId,
                DayOfWeek = (float)r.StartTime.DayOfWeek,
                Hour = r.StartTime.Hour,
                Capacity = r.Room.Capacity,
                UtilizationRate = (float)r.UtilizationRate
            }).ToList();

            var dataView = mlService.Context.Data.LoadFromEnumerable(usageData);

            var pipeline = mlService.Context.Transforms.Concatenate("Features",
                    nameof(RoomUsageData.RoomId),
                    nameof(RoomUsageData.DayOfWeek),
                    nameof(RoomUsageData.Hour),
                    nameof(RoomUsageData.Capacity))
                .Append(mlService.Context.Regression.Trainers.FastTree(
                    labelColumnName: nameof(RoomUsageData.UtilizationRate),
                    featureColumnName: "Features"));

            var model = pipeline.Fit(dataView);
            mlService.SaveModel(model, dataView.Schema, "RoomUsagePredictor");

            _logger.LogInformation("Modelo de predicción de uso de salas entrenado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entrenando modelo de uso de salas");
        }
    }

    private async Task TrainStudentPerformanceModel(FinancialDbContext context, MLModelService mlService)
    {
        try
        {
            var progressRecords = await context.StudentProgress
                .Include(sp => sp.Student)
                .OrderBy(sp => sp.AssessmentDate)
                .ToListAsync();

            if (progressRecords.Count < 10)
            {
                _logger.LogWarning("No hay suficientes datos de progreso estudiantil para entrenar el modelo");
                return;
            }

            var performanceData = progressRecords.Select((record, index) =>
            {
                var previousRecords = progressRecords
                    .Where(p => p.StudentId == record.StudentId && p.AssessmentDate < record.AssessmentDate)
                    .ToList();

                var performanceLevel = record.PerformanceLevel switch
                {
                    "Excellent" => 3f,
                    "Good" => 2f,
                    "Average" => 1f,
                    _ => 0f
                };

                return new StudentPerformanceData
                {
                    AttendanceRate = record.AttendanceRate,
                    PreviousScore = previousRecords.Any() ? (float)previousRecords.Last().Score : (float)record.Score,
                    DaysSinceEnrollment = (float)(record.AssessmentDate - record.Student.EnrollmentDate).TotalDays,
                    PerformanceLevel = performanceLevel
                };
            }).ToList();

            var dataView = mlService.Context.Data.LoadFromEnumerable(performanceData);

            var pipeline = mlService.Context.Transforms.Concatenate("Features",
                    nameof(StudentPerformanceData.AttendanceRate),
                    nameof(StudentPerformanceData.PreviousScore),
                    nameof(StudentPerformanceData.DaysSinceEnrollment))
                .Append(mlService.Context.Regression.Trainers.FastTree(
                    labelColumnName: nameof(StudentPerformanceData.PerformanceLevel),
                    featureColumnName: "Features"));

            var model = pipeline.Fit(dataView);
            mlService.SaveModel(model, dataView.Schema, "StudentPerformancePredictor");

            _logger.LogInformation("Modelo de predicción de rendimiento estudiantil entrenado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entrenando modelo de rendimiento estudiantil");
        }
    }
}
