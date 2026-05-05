using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using trip_tastic.Middleware;

namespace trip_tastic.Controllers;

/// <summary>
/// Toggle and inspect the WWW-Authenticate challenge behaviour.
/// When enabled, API requests without a Bearer token receive a 401 with a
/// WWW-Authenticate header pointing to the Protected Resource Metadata (RFC 9728),
/// following the MCP authorization flow.
///
/// Also serves the well-known metadata endpoints:
///   /.well-known/oauth-protected-resource   (RFC 9728 PRM document)
///   /.well-known/oauth-authorization-server  (RFC 8414 auth-server metadata)
/// </summary>
[ApiController]
[Produces("application/json")]
[AllowAnonymous]
public class WwwAuthChallengeController : ControllerBase
{
    private readonly WwwAuthChallengeState _state;
    private readonly IConfiguration _configuration;

    public WwwAuthChallengeController(WwwAuthChallengeState state, IConfiguration configuration)
    {
        _state = state;
        _configuration = configuration;
    }

    // ------------------------------------------------------------------
    //  Toggle / Status
    // ------------------------------------------------------------------

    /// <summary>
    /// Get the current WWW-Authenticate challenge state.
    /// </summary>
    [HttpGet("api/WwwAuthChallenge/status")]
    [ProducesResponseType(typeof(WwwAuthChallengeStatus), StatusCodes.Status200OK)]
    public ActionResult<WwwAuthChallengeStatus> GetStatus()
    {
        return Ok(BuildStatus());
    }

    /// <summary>
    /// Toggle WWW-Authenticate challenge on or off.
    /// Pass { "enabled": true/false } to set explicitly, or omit the body to toggle.
    /// </summary>
    [HttpPost("api/WwwAuthChallenge/toggle")]
    [ProducesResponseType(typeof(WwwAuthChallengeStatus), StatusCodes.Status200OK)]
    public ActionResult<WwwAuthChallengeStatus> Toggle([FromBody] WwwAuthToggleRequest? request = null)
    {
        if (request?.Enabled.HasValue == true)
        {
            if (request.Enabled.Value)
                _state.Enable();
            else
                _state.Disable();
        }
        else
        {
            _state.Toggle();
        }

        return Ok(BuildStatus());
    }

    // ------------------------------------------------------------------
    //  Well-Known Metadata Endpoints (RFC 9728 / RFC 8414)
    // ------------------------------------------------------------------

    /// <summary>
    /// Protected Resource Metadata (RFC 9728).
    /// Tells clients which authorization servers to use and what scopes are available.
    /// </summary>
    [HttpGet("/.well-known/oauth-protected-resource")]
    [ProducesResponseType(typeof(ProtectedResourceMetadata), StatusCodes.Status200OK)]
    public ActionResult<ProtectedResourceMetadata> GetProtectedResourceMetadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var tenantId = _configuration["AzureAd:TenantId"] ?? "{tenant}";
        var instance = _configuration["AzureAd:Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";

        return Ok(new ProtectedResourceMetadata
        {
            Resource = $"{baseUrl}/api",
            AuthorizationServers = new[] { $"{instance}/{tenantId}/v2.0" },
            ScopesSupported = new[] { "TripTastic.Admin", "TripTastic.User", "TripTastic.Reader" },
            BearerMethodsSupported = new[] { "header" },
            ResourceName = "TripTastic Travel API",
            ResourceDocumentation = $"{baseUrl}/swagger"
        });
    }

    /// <summary>
    /// OAuth 2.0 Authorization Server Metadata (RFC 8414).
    /// Provides discovery information for the authorization server endpoints.
    /// </summary>
    [HttpGet("/.well-known/oauth-authorization-server")]
    [ProducesResponseType(typeof(AuthorizationServerMetadata), StatusCodes.Status200OK)]
    public ActionResult<AuthorizationServerMetadata> GetAuthorizationServerMetadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var tenantId = _configuration["AzureAd:TenantId"] ?? "{tenant}";
        var instance = _configuration["AzureAd:Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";
        var clientId = _configuration["AzureAd:ClientId"] ?? "{client-id}";

        return Ok(new AuthorizationServerMetadata
        {
            Issuer = $"{instance}/{tenantId}/v2.0",
            AuthorizationEndpoint = $"{instance}/{tenantId}/oauth2/v2.0/authorize",
            TokenEndpoint = $"{instance}/{tenantId}/oauth2/v2.0/token",
            JwksUri = $"{instance}/{tenantId}/discovery/v2.0/keys",
            RegistrationEndpoint = null, // No DCR support by default
            ScopesSupported = new[] { "openid", "profile", "email", $"api://{clientId}/TripTastic.Admin", $"api://{clientId}/TripTastic.User", $"api://{clientId}/TripTastic.Reader" },
            ResponseTypesSupported = new[] { "code", "id_token", "code id_token" },
            GrantTypesSupported = new[] { "authorization_code", "implicit", "refresh_token" },
            TokenEndpointAuthMethodsSupported = new[] { "client_secret_basic", "client_secret_post" },
            ResourceMetadataUrl = $"{baseUrl}/.well-known/oauth-protected-resource"
        });
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------

    private WwwAuthChallengeStatus BuildStatus()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return new WwwAuthChallengeStatus
        {
            Enabled = _state.IsEnabled,
            ResourceMetadataUrl = $"{baseUrl}/.well-known/oauth-protected-resource",
            AuthorizationServerMetadataUrl = $"{baseUrl}/.well-known/oauth-authorization-server",
            Description = _state.IsEnabled
                ? "WWW-Authenticate challenges are ACTIVE. API requests without a Bearer token will receive 401 with resource_metadata."
                : "WWW-Authenticate challenges are INACTIVE. API requests are not challenged."
        };
    }
}

// ------------------------------------------------------------------
//  Response / Request Models
// ------------------------------------------------------------------

public record WwwAuthChallengeStatus
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("resourceMetadataUrl")]
    public required string ResourceMetadataUrl { get; init; }

    [JsonPropertyName("authorizationServerMetadataUrl")]
    public required string AuthorizationServerMetadataUrl { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}

public record WwwAuthToggleRequest
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }
}

/// <summary>
/// Protected Resource Metadata per RFC 9728 §3.
/// </summary>
public record ProtectedResourceMetadata
{
    [JsonPropertyName("resource")]
    public required string Resource { get; init; }

    [JsonPropertyName("authorization_servers")]
    public required string[] AuthorizationServers { get; init; }

    [JsonPropertyName("scopes_supported")]
    public required string[] ScopesSupported { get; init; }

    [JsonPropertyName("bearer_methods_supported")]
    public required string[] BearerMethodsSupported { get; init; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; init; }

    [JsonPropertyName("resource_documentation")]
    public string? ResourceDocumentation { get; init; }
}

/// <summary>
/// OAuth 2.0 Authorization Server Metadata per RFC 8414.
/// </summary>
public record AuthorizationServerMetadata
{
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    [JsonPropertyName("authorization_endpoint")]
    public required string AuthorizationEndpoint { get; init; }

    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    [JsonPropertyName("jwks_uri")]
    public required string JwksUri { get; init; }

    [JsonPropertyName("registration_endpoint")]
    public string? RegistrationEndpoint { get; init; }

    [JsonPropertyName("scopes_supported")]
    public required string[] ScopesSupported { get; init; }

    [JsonPropertyName("response_types_supported")]
    public required string[] ResponseTypesSupported { get; init; }

    [JsonPropertyName("grant_types_supported")]
    public required string[] GrantTypesSupported { get; init; }

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public required string[] TokenEndpointAuthMethodsSupported { get; init; }

    [JsonPropertyName("resource_metadata_url")]
    public string? ResourceMetadataUrl { get; init; }
}
