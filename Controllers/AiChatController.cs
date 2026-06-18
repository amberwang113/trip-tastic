using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trip_tastic.Models;

namespace trip_tastic.Controllers;

/// <summary>
/// Same-origin proxy for the chat widget. Forwards the browser's message to the
/// configured agent function (Azure Functions, SSE stream) and streams the
/// response straight back, so the function key never reaches the browser and
/// no cross-origin CORS is required. Exposed at POST /agent/chat (kept off the
/// /ai/chat path, which is owned by the easyagent site extension).
///
/// Configuration (appsettings or App Service connection/app settings):
///   AgentChat:Url         - base URL of the agent function's chatstream endpoint
///   AgentChat:FunctionKey - function key, appended as ?code=... (keep out of source;
///                           set via the AgentChat__FunctionKey app setting)
///
/// Session continuity is carried by the x-ms-session-id header, which is forwarded
/// to the function in both directions.
/// </summary>
[ApiController]
[Route("agent")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class AiChatController : ControllerBase
{
    private const string SessionHeader = "x-ms-session-id";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiChatController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task Chat(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["AgentChat:Url"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Response.WriteAsync("AgentChat:Url is not configured.", cancellationToken);
            return;
        }

        var requestUrl = BuildRequestUrl(baseUrl, _configuration["AgentChat:FunctionKey"]);

        // Buffer the incoming body so we can forward it verbatim (e.g. { "prompt": "..." }).
        Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };

        // Forward the session id so the agent continues the existing conversation.
        if (Request.Headers.TryGetValue(SessionHeader, out var incomingSession)
            && !string.IsNullOrEmpty(incomingSession))
        {
            upstreamRequest.Headers.TryAddWithoutValidation(SessionHeader, incomingSession.ToString());
        }

        var client = _httpClientFactory.CreateClient("AgentChat");
        client.Timeout = Timeout.InfiniteTimeSpan; // streaming response

        HttpResponseMessage upstreamResponse;
        try
        {
            upstreamResponse = await client.SendAsync(
                upstreamRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach agent chat endpoint.");
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsync("Unable to reach the chat service.", cancellationToken);
            return;
        }

        using (upstreamResponse)
        {
            Response.StatusCode = (int)upstreamResponse.StatusCode;
            Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString()
                                   ?? "text/event-stream";

            // Pass the session id back to the browser if the function returns it as a header.
            if (upstreamResponse.Headers.TryGetValues(SessionHeader, out var sessionValues))
            {
                Response.Headers[SessionHeader] = sessionValues.ToArray();
            }

            // Stream the SSE body through unbuffered so deltas arrive incrementally.
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            await using var upstreamStream =
                await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken);

            var buffer = new byte[8192];
            int read;
            while ((read = await upstreamStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await Response.Body.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }

    private static string BuildRequestUrl(string baseUrl, string? functionKey)
    {
        if (string.IsNullOrEmpty(functionKey))
        {
            return baseUrl;
        }

        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}code={Uri.EscapeDataString(functionKey)}";
    }
}
