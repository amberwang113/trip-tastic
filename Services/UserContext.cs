using System.Security.Claims;

namespace trip_tastic.Services;

/// <summary>
/// Implementation of IUserContext that extracts user identity from HTTP context.
/// Handles all three auth scenarios: anonymous, managed identity, and OBO.
/// </summary>
public class UserContext : IUserContext
{
    private const string AnonymousUserId = "anonymous";
    private const string AnonymousUserName = "Anonymous";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string UserId
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return AnonymousUserId;
            }

            // Try to get the 'oid' (object ID) claim first, then fall back to 'sub'
            var oid = User.FindFirst("oid")?.Value
                   ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

            return oid ?? AnonymousUserId;
        }
    }

    /// <inheritdoc />
    public string UserName
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return AnonymousUserName;
            }

            return User.FindFirst("name")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.Identity?.Name
                ?? AnonymousUserName;
        }
    }

    /// <inheritdoc />
    public string? UserEmail
    {
        get
        {
            return User?.FindFirst("preferred_username")?.Value
                ?? User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.FindFirst("email")?.Value;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public bool IsUserIdentity
    {
        get
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            // Check the 'idtyp' claim - if it's 'user', this is an OBO token
            var idType = User?.FindFirst("idtyp")?.Value;
            return string.Equals(idType, "user", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public bool IsManagedIdentity
    {
        get
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            // Check the 'idtyp' claim - if it's 'app', this is a managed identity token
            var idType = User?.FindFirst("idtyp")?.Value;
            return string.Equals(idType, "app", StringComparison.OrdinalIgnoreCase);
        }
    }
}
