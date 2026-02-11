using System.Diagnostics;
using trip_tastic.Services;

namespace trip_tastic.Middleware;

/// <summary>
/// Middleware that logs all incoming requests for debugging.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestLogService _logService;

    public RequestLoggingMiddleware(RequestDelegate next, RequestLogService logService)
    {
        _next = next;
        _logService = logService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Capture request info before processing
        var entry = CaptureRequestInfo(context);
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            entry.DurationMs = stopwatch.ElapsedMilliseconds;
            entry.StatusCode = context.Response.StatusCode;
            
            // Don't log static files or the debug page itself to reduce noise
            if (!ShouldSkipLogging(entry.Path))
            {
                _logService.Log(entry);
            }
        }
    }

    private RequestLogEntry CaptureRequestInfo(HttpContext context)
    {
        var request = context.Request;
        var headers = request.Headers;

        var entry = new RequestLogEntry
        {
            Method = request.Method,
            Path = request.Path.Value ?? "/",
            QueryString = request.QueryString.HasValue ? request.QueryString.Value : null,
            
            // Request details
            UserAgent = headers.UserAgent.FirstOrDefault() ?? "[NOT PROVIDED]",
            UserAgentSource = DetermineUserAgentSource(headers),
            RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
            Referer = headers.Referer.FirstOrDefault(),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            
            // EasyAuth headers
            EasyAuthPrincipalId = headers["X-MS-CLIENT-PRINCIPAL-ID"].FirstOrDefault(),
            EasyAuthPrincipalName = headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault(),
            EasyAuthIdp = headers["X-MS-CLIENT-PRINCIPAL-IDP"].FirstOrDefault(),
            HasAccessToken = !string.IsNullOrEmpty(headers["X-MS-TOKEN-AAD-ACCESS-TOKEN"].FirstOrDefault()),
            HasIdToken = !string.IsNullOrEmpty(headers["X-MS-TOKEN-AAD-ID-TOKEN"].FirstOrDefault()),
        };

        // Check authorization header
        var authHeader = headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                entry.AuthorizationHeader = "Bearer [REDACTED]";
            }
            else
            {
                entry.AuthorizationHeader = "[PRESENT - REDACTED]";
            }
        }

        // Determine authentication status
        entry.IsAuthenticated = !string.IsNullOrEmpty(entry.EasyAuthPrincipalId) 
                               || context.User?.Identity?.IsAuthenticated == true;
        
        // Get user info from EasyAuth headers or claims
        if (!string.IsNullOrEmpty(entry.EasyAuthPrincipalId))
        {
            entry.UserId = entry.EasyAuthPrincipalId;
            entry.UserName = entry.EasyAuthPrincipalName;
            entry.UserEmail = entry.EasyAuthPrincipalName?.Contains('@') == true 
                ? entry.EasyAuthPrincipalName : null;
            entry.IdentityProvider = entry.EasyAuthIdp;
            entry.IdentityType = "user"; // EasyAuth headers are typically from users
        }
        else if (context.User?.Identity?.IsAuthenticated == true)
        {
            entry.UserId = context.User.FindFirst("oid")?.Value 
                        ?? context.User.FindFirst("sub")?.Value;
            entry.UserName = context.User.FindFirst("name")?.Value 
                          ?? context.User.Identity?.Name;
            entry.UserEmail = context.User.FindFirst("preferred_username")?.Value 
                           ?? context.User.FindFirst("email")?.Value;
            entry.IdentityType = context.User.FindFirst("idtyp")?.Value;
        }

        // Capture other interesting headers
        var interestingHeaderNames = new[]
        {
            "X-Forwarded-For",
            "X-Forwarded-Proto",
            "X-Original-URL",
            "X-ARR-LOG-ID",
            "X-SITE-DEPLOYMENT-ID",
            "X-Requested-With",
            "Origin",
            "Accept"
        };

        foreach (var headerName in interestingHeaderNames)
        {
            var value = headers[headerName].FirstOrDefault();
            if (!string.IsNullOrEmpty(value))
            {
                entry.InterestingHeaders[headerName] = value;
            }
        }

        return entry;
    }

    private static string DetermineUserAgentSource(IHeaderDictionary headers)
    {
        var userAgent = headers.UserAgent.FirstOrDefault();
        
        if (string.IsNullOrEmpty(userAgent))
        {
            // No User-Agent - likely a programmatic client like VS Code MCP
            // Check for other indicators
            var authHeader = headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return "API Client (Bearer Token, No UA)";
            }
            return "Unknown (No User-Agent)";
        }
        
        var ua = userAgent.ToLowerInvariant();
        
        // Check for VS Code related clients
        if (ua.Contains("vscode") || ua.Contains("vs code") || ua.Contains("visual studio code"))
            return "VS Code";
        
        // Check for REST clients
        if (ua.Contains("rest-client") || ua.Contains("restclient") || ua.Contains("insomnia") || ua.Contains("postman"))
            return "REST Client";
        
        // Check for common browsers
        if (ua.Contains("mozilla") || ua.Contains("chrome") || ua.Contains("safari") || ua.Contains("edge") || ua.Contains("firefox"))
            return "Browser";
        
        // Check for curl or other CLI tools
        if (ua.Contains("curl") || ua.Contains("wget") || ua.Contains("httpie"))
            return "CLI Tool";
        
        // Check for .NET HttpClient
        if (ua.Contains("dotnet") || ua.Contains(".net"))
            return ".NET HttpClient";
        
        return $"Other ({userAgent.Substring(0, Math.Min(30, userAgent.Length))})";
    }

    private static bool ShouldSkipLogging(string path)
    {
        // Skip static files and noisy endpoints
        var skipPrefixes = new[]
        {
            "/lib/",
            "/css/",
            "/js/",
            "/images/",
            "/_framework/",
            "/_vs/",
            "/_blazor",
            "/favicon",
            "/.well-known/"
        };

        var skipSuffixes = new[]
        {
            ".css",
            ".js",
            ".map",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".svg",
            ".ico",
            ".woff",
            ".woff2",
            ".ttf",
            ".eot"
        };

        var pathLower = path.ToLowerInvariant();

        return skipPrefixes.Any(prefix => pathLower.StartsWith(prefix))
            || skipSuffixes.Any(suffix => pathLower.EndsWith(suffix));
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
