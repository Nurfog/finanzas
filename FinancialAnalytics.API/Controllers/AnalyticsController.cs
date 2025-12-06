using Microsoft.AspNetCore.Mvc;
using FinancialAnalytics.API.Services;

namespace FinancialAnalytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(AnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get revenue analytics for a date range
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _analyticsService.GetRevenueAnalytics(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get revenue analytics by location
    /// </summary>
    [HttpGet("revenue/by-location")]
    public async Task<IActionResult> GetRevenueByLocation(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _analyticsService.GetRevenueByLocation(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue by location");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Predict future revenue for a location
    /// </summary>
    [HttpGet("revenue/predictions")]
    public async Task<IActionResult> PredictRevenue(
        [FromQuery] int locationId,
        [FromQuery] int monthsAhead = 3)
    {
        try
        {
            var result = await _analyticsService.PredictRevenue(locationId, monthsAhead);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting revenue");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get customer segmentation analysis
    /// </summary>

    public async Task<IActionResult> GetCustomerSegments(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _analyticsService.GetCustomerSegments(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer segments");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get room usage analytics
    /// </summary>
    [HttpGet("rooms/usage")]
    public async Task<IActionResult> GetRoomUsageAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _analyticsService.GetRoomUsageAnalytics(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room usage analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get student performance analytics
    /// </summary>
    [HttpGet("students/performance")]
    public async Task<IActionResult> GetStudentAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _analyticsService.GetStudentAnalytics(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get room utilization by location and period
    /// </summary>
    [HttpGet("rooms/utilization")]
    public async Task<IActionResult> GetRoomUtilization(
        [FromQuery] int? locationId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.Now.AddMonths(-3);
            var end = endDate ?? DateTime.Now;
            var result = await _analyticsService.GetRoomUtilizationByLocation(locationId, start, end);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room utilization");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get room usage patterns by day of week
    /// </summary>
    [HttpGet("rooms/patterns")]
    public async Task<IActionResult> GetRoomPatterns([FromQuery] int? locationId = null)
    {
        try
        {
            var result = await _analyticsService.GetRoomUsagePatternsByDayOfWeek(locationId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room usage patterns");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get underutilized rooms
    /// </summary>
    [HttpGet("rooms/underutilized")]
    public async Task<IActionResult> GetUnderutilizedRooms([FromQuery] decimal threshold = 0.5m)
    {
        try
        {
            var result = await _analyticsService.GetUnderutilizedRooms(threshold);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting underutilized rooms");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get room optimization suggestions
    /// </summary>
    [HttpGet("rooms/optimization")]
    public async Task<IActionResult> GetRoomOptimizationSuggestions()
    {
        try
        {
            var result = await _analyticsService.GetRoomOptimizationSuggestions();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room optimization suggestions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
