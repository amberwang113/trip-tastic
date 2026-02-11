using Microsoft.AspNetCore.Mvc;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

/// <summary>
/// API endpoints for the request debug log.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DebugLogController : ControllerBase
{
    private readonly RequestLogService _logService;

    public DebugLogController(RequestLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// Get recent request logs.
    /// </summary>
    [HttpGet]
    public ActionResult<object> GetLogs([FromQuery] int limit = 100)
    {
        var logs = _logService.GetLogs(limit);
        return Ok(new
        {
            Count = logs.Count,
            Total = _logService.Count,
            Logs = logs
        });
    }

    /// <summary>
    /// Clear all logs.
    /// </summary>
    [HttpDelete]
    public ActionResult ClearLogs()
    {
        _logService.Clear();
        return Ok(new { Message = "Logs cleared" });
    }

    /// <summary>
    /// Get summary statistics.
    /// </summary>
    [HttpGet("stats")]
    public ActionResult<object> GetStats()
    {
        var logs = _logService.GetLogs();
        
        var stats = new
        {
            TotalRequests = logs.Count,
            AuthenticatedRequests = logs.Count(l => l.IsAuthenticated),
            AnonymousRequests = logs.Count(l => !l.IsAuthenticated),
            
            ByStatusCode = logs
                .GroupBy(l => l.StatusCode / 100 * 100) // Group by 2xx, 3xx, 4xx, 5xx
                .ToDictionary(g => $"{g.Key}s", g => g.Count()),
            
            ByMethod = logs
                .GroupBy(l => l.Method)
                .ToDictionary(g => g.Key, g => g.Count()),
            
            ByUser = logs
                .Where(l => l.IsAuthenticated)
                .GroupBy(l => l.UserName ?? l.UserId ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            
            AverageDurationMs = logs.Count > 0 ? logs.Average(l => l.DurationMs) : 0,
            
            UniqueUsers = logs
                .Where(l => l.IsAuthenticated)
                .Select(l => l.UserId)
                .Distinct()
                .Count(),
            
            RequestsWithBearerToken = logs.Count(l => !string.IsNullOrEmpty(l.AuthorizationHeader)),
            RequestsWithEasyAuth = logs.Count(l => !string.IsNullOrEmpty(l.EasyAuthPrincipalId))
        };

        return Ok(stats);
    }
}
