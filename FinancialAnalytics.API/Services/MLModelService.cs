using Microsoft.ML;
using Microsoft.ML.Data;

namespace FinancialAnalytics.API.Services;

public class MLModelService
{
    private readonly MLContext _mlContext;
    private readonly string _modelsPath;
    private readonly ILogger<MLModelService> _logger;

    public MLModelService(IConfiguration configuration, ILogger<MLModelService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _modelsPath = configuration["MLSettings:ModelsPath"] ?? "MLModels/Trained";
        _logger = logger;

        // Ensure models directory exists
        Directory.CreateDirectory(_modelsPath);
    }

    public MLContext Context => _mlContext;
    public string ModelsPath => _modelsPath;

    public void SaveModel(ITransformer model, DataViewSchema schema, string modelName)
    {
        var modelPath = Path.Combine(_modelsPath, $"{modelName}.zip");
        _mlContext.Model.Save(model, schema, modelPath);
        _logger.LogInformation($"Model saved: {modelPath}");
    }

    public ITransformer? LoadModel(string modelName, out DataViewSchema schema)
    {
        var modelPath = Path.Combine(_modelsPath, $"{modelName}.zip");
        
        if (!File.Exists(modelPath))
        {
            schema = null!;
            return null;
        }

        var model = _mlContext.Model.Load(modelPath, out schema);
        _logger.LogInformation($"Model loaded: {modelPath}");
        return model;
    }

    public bool ModelExists(string modelName)
    {
        var modelPath = Path.Combine(_modelsPath, $"{modelName}.zip");
        return File.Exists(modelPath);
    }
}

// ML Models for Revenue Prediction
public class RevenueData
{
    [LoadColumn(0)]
    public float Month { get; set; }

    [LoadColumn(1)]
    public float LocationId { get; set; }

    [LoadColumn(2)]
    public float CustomerCount { get; set; }

    [LoadColumn(3)]
    public float TransactionCount { get; set; }

    [LoadColumn(4)]
    public float Revenue { get; set; }
}

public class RevenuePrediction
{
    [ColumnName("Score")]
    public float PredictedRevenue { get; set; }
}

// ML Models for Customer Segmentation
public class CustomerData
{
    [LoadColumn(0)]
    public float CustomerId { get; set; }

    [LoadColumn(1)]
    public float TotalSpent { get; set; }

    [LoadColumn(2)]
    public float TransactionFrequency { get; set; }

    [LoadColumn(3)]
    public float DaysSinceRegistration { get; set; }

    [LoadColumn(4)]
    public float AverageTransactionValue { get; set; }
}

public class CustomerSegmentation
{
    [ColumnName("PredictedLabel")]
    public uint Segment { get; set; }

    [ColumnName("Score")]
    public float[] Distances { get; set; } = Array.Empty<float>();
}

// ML Models for Room Usage Prediction
public class RoomUsageData
{
    [LoadColumn(0)]
    public float RoomId { get; set; }

    [LoadColumn(1)]
    public float DayOfWeek { get; set; }

    [LoadColumn(2)]
    public float Hour { get; set; }

    [LoadColumn(3)]
    public float Capacity { get; set; }

    [LoadColumn(4)]
    public float UtilizationRate { get; set; }
}

public class RoomUsagePrediction
{
    [ColumnName("Score")]
    public float PredictedUtilization { get; set; }
}

// ML Models for Student Performance
public class StudentPerformanceData
{
    [LoadColumn(0)]
    public float AttendanceRate { get; set; }

    [LoadColumn(1)]
    public float PreviousScore { get; set; }

    [LoadColumn(2)]
    public float DaysSinceEnrollment { get; set; }

    [LoadColumn(3)]
    public float PerformanceLevel { get; set; } // 0=Poor, 1=Average, 2=Good, 3=Excellent
}

public class StudentPerformancePrediction
{
    [ColumnName("PredictedLabel")]
    public float PredictedPerformance { get; set; }

    [ColumnName("Score")]
    public float[] Probabilities { get; set; } = Array.Empty<float>();
}
