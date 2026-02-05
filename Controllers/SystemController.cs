using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace trip_tastic.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    /// <summary>
    /// Get the current date and time information.
    /// This is useful for AI agents to understand the current temporal context when planning trips.
    /// </summary>
    /// <returns>Current date and time information</returns>
    [HttpGet("current-date")]
    [ProducesResponseType(typeof(CurrentDateResponse), StatusCodes.Status200OK)]
    public ActionResult<CurrentDateResponse> GetCurrentDate()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        
        return Ok(new CurrentDateResponse
        {
            CurrentDateUtc = today.ToString("yyyy-MM-dd"),
            CurrentDateTimeUtc = now,
            CurrentDateFormatted = today.ToString("yyyy-MM-dd"),
            DayOfWeek = today.DayOfWeek.ToString(),
            Tomorrow = today.AddDays(1).ToString("yyyy-MM-dd"),
            NextWeek = today.AddDays(7).ToString("yyyy-MM-dd"),
            NextMonth = today.AddMonths(1).ToString("yyyy-MM-dd")
        });
    }
}

/// <summary>
/// Response containing current date and time information
/// </summary>
public record CurrentDateResponse
{
    /// <summary>
    /// Current date in UTC (without time component) as YYYY-MM-DD string
    /// </summary>
    [JsonPropertyName("currentDateUtc")]
    public required string CurrentDateUtc { get; init; }
    
    /// <summary>
    /// Current date and time in UTC
    /// </summary>
    [JsonPropertyName("currentDateTimeUtc")]
    public required DateTime CurrentDateTimeUtc { get; init; }
    
    /// <summary>
    /// Current date formatted as YYYY-MM-DD string
    /// </summary>
    [JsonPropertyName("currentDateFormatted")]
    public required string CurrentDateFormatted { get; init; }
    
    /// <summary>
    /// Current day of the week (e.g., "Monday", "Tuesday")
    /// </summary>
    [JsonPropertyName("dayOfWeek")]
    public required string DayOfWeek { get; init; }
    
    /// <summary>
    /// Tomorrow's date as YYYY-MM-DD string - useful for booking flights/hotels starting tomorrow
    /// </summary>
    [JsonPropertyName("tomorrow")]
    public required string Tomorrow { get; init; }
    
    /// <summary>
    /// Date one week from now as YYYY-MM-DD string - useful for planning short trips
    /// </summary>
    [JsonPropertyName("nextWeek")]
    public required string NextWeek { get; init; }
    
    /// <summary>
    /// Date one month from now as YYYY-MM-DD string - useful for planning longer trips
    /// </summary>
    [JsonPropertyName("nextMonth")]
    public required string NextMonth { get; init; }
}
