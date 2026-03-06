using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace trip_tastic.Services;

/// <summary>
/// Development-only authentication handler that creates a ClaimsPrincipal
/// from the DevUser cookie. This bridges the dev user switcher into the
/// standard ASP.NET Core authentication pipeline so [Authorize] attributes
/// and role policies work without real JWT tokens.
///
/// In production this handler is never registered.
/// </summary>
public class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string DevUserCookieName = "DevUser";

    public const string SchemeName = "DevAuth";

    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var cookie = Request.Cookies[DevUserCookieName];
        var devUser = string.IsNullOrEmpty(cookie)
            ? null
            : DevUserContext.AvailableUsers.FirstOrDefault(u => u.Id == cookie);

        // No cookie or "anonymous" → no identity (unauthenticated)
        if (devUser is null || devUser.Id == "anonymous")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, devUser.Id),
            new("oid", devUser.Id),
            new(ClaimTypes.Name, devUser.Name),
            new("name", devUser.Name),
            // In dev mode, grant all roles so every endpoint is reachable
            new(ClaimTypes.Role, "TripTastic.Admin"),
            new(ClaimTypes.Role, "TripTastic.User"),
            new(ClaimTypes.Role, "TripTastic.Reader"),
            new("idtyp", "user"),
        };

        if (devUser.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, devUser.Email));
            claims.Add(new Claim("preferred_username", devUser.Email));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
