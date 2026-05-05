using Microsoft.Net.Http.Headers;

namespace trip_tastic.Middleware;

/// <summary>
/// When enabled at runtime, this middleware intercepts API requests that lack a valid
/// Bearer token and returns 401 with a WWW-Authenticate header pointing to the
/// Protected Resource Metadata document (RFC 9728), following the MCP authorization flow.
///
/// Toggle on/off via POST /api/WwwAuthChallenge/toggle or the EnableWwwAuthenticate config.
/// </summary>
public class WwwAuthChallengeMiddleware
{
    private readonly RequestDelegate _next;

    public WwwAuthChallengeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var state = context.RequestServices.GetRequiredService<WwwAuthChallengeState>();

        if (!state.IsEnabled)
        {
            await _next(context);
            return;
        }

        // Only challenge API routes (not Razor Pages / static files)
        var path = context.Request.Path.Value ?? "";
        bool isApiRoute = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        bool isWellKnown = path.StartsWith("/.well-known/", StringComparison.OrdinalIgnoreCase);

        // Don't challenge the toggle/status endpoints or well-known metadata endpoints themselves
        if (!isApiRoute || isWellKnown
            || path.StartsWith("/api/WwwAuthChallenge", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // If the request has a Bearer token, let the normal auth pipeline handle it
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Build the resource_metadata URL from the current request
        var scheme = context.Request.Scheme;
        var host = context.Request.Host.ToString();
        var resourceMetadataUrl = $"{scheme}://{host}/.well-known/oauth-protected-resource";

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.Append(
            HeaderNames.WWWAuthenticate,
            $"Bearer realm=\"trip-tastic\", resource_metadata=\"{resourceMetadataUrl}\"");

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "unauthorized",
            message = "Bearer token required. See WWW-Authenticate header for authorization metadata.",
            resourceMetadata = resourceMetadataUrl
        });
    }
}

/// <summary>
/// Singleton holding the runtime on/off state for WWW-Authenticate challenges.
/// </summary>
public class WwwAuthChallengeState
{
    private volatile bool _enabled;

    public WwwAuthChallengeState(bool initiallyEnabled)
    {
        _enabled = initiallyEnabled;
    }

    public bool IsEnabled => _enabled;

    public void Enable() => _enabled = true;
    public void Disable() => _enabled = false;
    public void Toggle() => _enabled = !_enabled;
}

public static class WwwAuthChallengeMiddlewareExtensions
{
    public static IApplicationBuilder UseWwwAuthChallenge(this IApplicationBuilder app)
    {
        return app.UseMiddleware<WwwAuthChallengeMiddleware>();
    }
}
