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
    [HttpGet("customers/segments")]
    public async Task<IActionResult> GetCustomerSegments()
    {
        try
        {
            var result = await _analyticsService.GetCustomerSegments();
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
    public async Task<IActionResult> GetRoomUsageAnalytics()
    {
        try
        {
            var result = await _analyticsService.GetRoomUsageAnalytics();
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
    public async Task<IActionResult> GetStudentAnalytics()
    {
        try
        {
            var result = await _analyticsService.GetStudentAnalytics();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student analytics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
