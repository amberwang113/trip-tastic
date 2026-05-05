using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace trip_tastic.Services;

/// <summary>
/// Authentication handler for Azure App Service EasyAuth (built-in authentication).
/// When the app runs behind App Service authentication, the platform strips tokens and
/// forwards identity information via X-MS-CLIENT-PRINCIPAL and related headers.
///
/// This handler reads those headers and builds a ClaimsPrincipal so the rest of
/// the ASP.NET Core auth/authz pipeline works normally.
/// </summary>
public class EasyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "EasyAuth";
    private const string PrincipalHeader = "X-MS-CLIENT-PRINCIPAL";
    private const string PrincipalIdHeader = "X-MS-CLIENT-PRINCIPAL-ID";
    private const string PrincipalNameHeader = "X-MS-CLIENT-PRINCIPAL-NAME";
    private const string PrincipalIdpHeader = "X-MS-CLIENT-PRINCIPAL-IDP";

    public EasyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If there's no EasyAuth header, let other handlers take over
        if (!Request.Headers.ContainsKey(PrincipalHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var principal = ParseClientPrincipal();
            if (principal == null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var ticket = new AuthenticationTicket(principal, SchemeName);
            Logger.LogInformation(
                "EasyAuth: authenticated user {UserId} via {Idp}",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
                Request.Headers[PrincipalIdpHeader].FirstOrDefault() ?? "unknown");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "EasyAuth: failed to parse X-MS-CLIENT-PRINCIPAL header");
            return Task.FromResult(AuthenticateResult.Fail("Invalid EasyAuth principal header."));
        }
    }

    private ClaimsPrincipal? ParseClientPrincipal()
    {
        var headerValue = Request.Headers[PrincipalHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }

        // X-MS-CLIENT-PRINCIPAL is a Base64-encoded JSON payload
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));
        var principalData = JsonSerializer.Deserialize<EasyAuthPrincipal>(decoded);

        if (principalData?.Claims == null || principalData.Claims.Length == 0)
        {
            return null;
        }

        // Map the claims from EasyAuth format to standard ClaimsPrincipal
        var claims = new List<Claim>();
        foreach (var c in principalData.Claims)
        {
            // EasyAuth may send multiple values for the same claim type (e.g. roles)
            claims.Add(new Claim(c.Type, c.Value));
        }

        // Ensure we have fallback identity claims from the simpler headers
        if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
            var principalId = Request.Headers[PrincipalIdHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(principalId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, principalId));
            }
        }

        if (!claims.Any(c => c.Type == ClaimTypes.Name || c.Type == "name"))
        {
            var principalName = Request.Headers[PrincipalNameHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(principalName))
            {
                claims.Add(new Claim(ClaimTypes.Name, principalName));
            }
        }

        // Mark identity type for UserContext compatibility
        if (!claims.Any(c => c.Type == "idtyp"))
        {
            claims.Add(new Claim("idtyp", "user"));
        }

        var identity = new ClaimsIdentity(
            claims,
            SchemeName,
            principalData.NameType ?? ClaimTypes.Name,
            principalData.RoleType ?? ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Mirrors the JSON structure sent by App Service in X-MS-CLIENT-PRINCIPAL.
    /// App Service uses snake_case abbreviated keys: auth_typ, name_typ, role_typ,
    /// and claims with typ/val.
    /// </summary>
    private sealed class EasyAuthPrincipal
    {
        [JsonPropertyName("auth_typ")]
        public string? AuthType { get; set; }

        [JsonPropertyName("claims")]
        public EasyAuthClaim[] Claims { get; set; } = Array.Empty<EasyAuthClaim>();

        [JsonPropertyName("name_typ")]
        public string? NameType { get; set; }

        [JsonPropertyName("role_typ")]
        public string? RoleType { get; set; }
    }

    private sealed class EasyAuthClaim
    {
        [JsonPropertyName("typ")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("val")]
        public string Value { get; set; } = string.Empty;
    }
}
