using System.Collections.Concurrent;

namespace trip_tastic.Services;

/// <summary>
/// In-memory service that logs all incoming requests for debugging purposes.
/// </summary>
public class RequestLogService
{
    private readonly ConcurrentQueue<RequestLogEntry> _logs = new();
    private const int MaxLogEntries = 500;

    public void Log(RequestLogEntry entry)
    {
        _logs.Enqueue(entry);
        
        // Keep only the last N entries
        while (_logs.Count > MaxLogEntries && _logs.TryDequeue(out _)) { }
    }

    public IReadOnlyList<RequestLogEntry> GetLogs(int? limit = null)
    {
        var logs = _logs.ToArray().Reverse().ToList();
        return limit.HasValue ? logs.Take(limit.Value).ToList() : logs;
    }

    public void Clear()
    {
        while (_logs.TryDequeue(out _)) { }
    }

    public int Count => _logs.Count;
}

public class RequestLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    
    // Authentication info
    public bool IsAuthenticated { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? IdentityProvider { get; set; }
    public string? IdentityType { get; set; } // "user", "app", or null
    
    // EasyAuth headers
    public string? EasyAuthPrincipalId { get; set; }
    public string? EasyAuthPrincipalName { get; set; }
    public string? EasyAuthIdp { get; set; }
    public bool HasAccessToken { get; set; }
    public bool HasIdToken { get; set; }
    
    // Request details
    public string? UserAgent { get; set; }
    public string? UserAgentSource { get; set; } // "Browser", "API Client", "VS Code", "Unknown"
    public string? RemoteIp { get; set; }
    public string? Referer { get; set; }
    public string? ContentType { get; set; }
    public long? ContentLength { get; set; }
    
    // Authorization header (redacted)
    public string? AuthorizationHeader { get; set; }
    
    // Additional headers of interest
    public Dictionary<string, string> InterestingHeaders { get; set; } = new();
}
