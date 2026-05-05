using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace trip_tastic.Controllers;

/// <summary>
/// Exposes authentication metadata so other services can validate tokens
/// issued for this application. This is the public contract that downstream
/// services use to verify JWT tokens presented by callers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the site authentication metadata required by other services
    /// to validate JWT tokens scoped to this application.
    ///
    /// Other services can use this response to configure their own JWT
    /// validation middleware (JWKS URI for signature verification, issuer
    /// for issuer validation, audience to confirm the token was intended
    /// for this app, and scopes for permission checks).
    /// </summary>
    /// <returns>Authentication metadata for token validation</returns>
    [HttpGet("site-auth")]
    [ProducesResponseType(typeof(SiteAuthMetadata), StatusCodes.Status200OK)]
    public ActionResult<SiteAuthMetadata> GetSiteAuthMetadata()
    {
        var tenantId = _configuration["AzureAd:TenantId"] ?? "{tenant}";
        var instance = _configuration["AzureAd:Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";
        var clientId = _configuration["AzureAd:ClientId"] ?? "{client-id}";
        var audience = _configuration["AzureAd:Audience"] ?? $"api://{clientId}";
        var scopes = _configuration["AzureAd:Scopes"] ?? "user_impersonation";

        return Ok(new SiteAuthMetadata
        {
            JwksUri = $"{instance}/{tenantId}/discovery/v2.0/keys",
            Issuer = $"{instance}/{tenantId}/v2.0",
            Audience = audience,
            AuthorizationServerMetadataUrl = $"{instance}/{tenantId}/v2.0/.well-known/openid-configuration",
            Scopes = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Contains("://") ? s : $"api://{clientId}/{s}")
                           .ToArray()
        });
    }

    /// <summary>
    /// Returns the RBAC roles and policies configured in this application.
    /// Useful for other services that need to understand the permission model.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(RolesMetadata), StatusCodes.Status200OK)]
    public ActionResult<RolesMetadata> GetRolesMetadata()
    {
        return Ok(new RolesMetadata
        {
            Roles = new[]
            {
                new RoleInfo
                {
                    Value = "TripTastic.Admin",
                    DisplayName = "Administrator",
                    Description = "Full access: manage all resources, access admin/debug endpoints, view all user data."
                },
                new RoleInfo
                {
                    Value = "TripTastic.User",
                    DisplayName = "User",
                    Description = "Standard user: search flights/hotels, manage own cart, book and manage own trips."
                },
                new RoleInfo
                {
                    Value = "TripTastic.Reader",
                    DisplayName = "Reader",
                    Description = "Read-only: browse flights, hotels, and destinations. Cannot book or modify cart."
                }
            },
            Policies = new[]
            {
                new PolicyInfo { Name = "RequireAdmin", AllowedRoles = Array.Empty<string>(), Note = "Any authenticated user (roles disabled)." },
                new PolicyInfo { Name = "RequireUser", AllowedRoles = Array.Empty<string>(), Note = "Any authenticated user (roles disabled)." },
                new PolicyInfo { Name = "RequireReader", AllowedRoles = Array.Empty<string>(), Note = "Any authenticated user (roles disabled)." },
                new PolicyInfo { Name = "RequireAuthenticated", AllowedRoles = Array.Empty<string>(), Note = "Any authenticated user." }
            }
        });
    }

    /// <summary>
    /// Returns the current user's authentication info including roles.
    /// Requires authentication.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserAuthInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserAuthInfo> GetCurrentUser()
    {
        var user = HttpContext.User;

        return Ok(new UserAuthInfo
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            UserId = user.FindFirst("oid")?.Value
                  ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                  ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            UserName = user.FindFirst("name")?.Value
                    ?? user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? user.Identity?.Name,
            Email = user.FindFirst("preferred_username")?.Value
                 ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            Roles = user.Claims
                        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "roles")
                        .Select(c => c.Value)
                        .Distinct()
                        .ToArray(),
            IdentityType = user.FindFirst("idtyp")?.Value ?? "unknown",
            TokenIssuer = user.FindFirst("iss")?.Value
        });
    }
}

#region Response Models

/// <summary>
/// Authentication metadata that other services use to validate JWT tokens
/// issued for this application. Matches the siteAuth contract.
/// </summary>
public record SiteAuthMetadata
{
    /// <summary>
    /// URI to fetch the JSON Web Key Set for verifying token signatures.
    /// </summary>
    [JsonPropertyName("jwksUri")]
    public required string JwksUri { get; init; }

    /// <summary>
    /// Expected token issuer for issuer validation.
    /// </summary>
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    /// <summary>
    /// Expected audience claim value in the token.
    /// </summary>
    [JsonPropertyName("audience")]
    public required string Audience { get; init; }

    /// <summary>
    /// OpenID Connect discovery document URL.
    /// </summary>
    [JsonPropertyName("authorizationServerMetadataUrl")]
    public required string AuthorizationServerMetadataUrl { get; init; }

    /// <summary>
    /// Scopes accepted by this application.
    /// </summary>
    [JsonPropertyName("scopes")]
    public required string[] Scopes { get; init; }
}

public record RolesMetadata
{
    [JsonPropertyName("roles")]
    public required RoleInfo[] Roles { get; init; }

    [JsonPropertyName("policies")]
    public required PolicyInfo[] Policies { get; init; }
}

public record RoleInfo
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}

public record PolicyInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("allowedRoles")]
    public required string[] AllowedRoles { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }
}

public record UserAuthInfo
{
    [JsonPropertyName("isAuthenticated")]
    public bool IsAuthenticated { get; init; }

    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    [JsonPropertyName("userName")]
    public string? UserName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("roles")]
    public required string[] Roles { get; init; }

    [JsonPropertyName("identityType")]
    public string? IdentityType { get; init; }

    [JsonPropertyName("tokenIssuer")]
    public string? TokenIssuer { get; init; }
}

#endregion
