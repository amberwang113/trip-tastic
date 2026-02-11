using System.Security.Claims;

namespace trip_tastic.Services;

/// <summary>
/// Implementation of IUserContext that extracts user identity from HTTP context.
/// Handles all three auth scenarios: anonymous, managed identity, and OBO.
/// 
/// Supports two sources of identity:
/// 1. Standard ClaimsPrincipal (when JWT bearer auth middleware is configured)
/// 2. EasyAuth headers (X-MS-CLIENT-PRINCIPAL-* headers injected by Azure App Service)
/// </summary>
public class UserContext : IUserContext
{
    private const string AnonymousUserId = "anonymous";
    private const string AnonymousUserName = "Anonymous";

    // EasyAuth header names
    private const string EasyAuthPrincipalIdHeader = "X-MS-CLIENT-PRINCIPAL-ID";
    private const string EasyAuthPrincipalNameHeader = "X-MS-CLIENT-PRINCIPAL-NAME";
    private const string EasyAuthPrincipalIdpHeader = "X-MS-CLIENT-PRINCIPAL-IDP";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;
    private ClaimsPrincipal? User => HttpContext?.User;

    /// <summary>
    /// Gets the user ID from EasyAuth headers if present.
    /// </summary>
    private string? EasyAuthUserId => HttpContext?.Request.Headers[EasyAuthPrincipalIdHeader].FirstOrDefault();

    /// <summary>
    /// Gets the user name from EasyAuth headers if present.
    /// </summary>
    private string? EasyAuthUserName => HttpContext?.Request.Headers[EasyAuthPrincipalNameHeader].FirstOrDefault();

    /// <summary>
    /// Gets the identity provider from EasyAuth headers if present.
    /// </summary>
    private string? EasyAuthIdp => HttpContext?.Request.Headers[EasyAuthPrincipalIdpHeader].FirstOrDefault();

    /// <summary>
    /// Checks if EasyAuth headers are present (user is authenticated via Azure App Service).
    /// </summary>
    private bool HasEasyAuthHeaders => !string.IsNullOrEmpty(EasyAuthUserId);

    /// <inheritdoc />
    public string UserId
    {
        get
        {
            // First check EasyAuth headers (Azure App Service authentication)
            if (HasEasyAuthHeaders)
            {
                return EasyAuthUserId!;
            }

            // Fall back to ClaimsPrincipal (JWT bearer auth or other middleware)
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
            // First check EasyAuth headers
            if (HasEasyAuthHeaders && !string.IsNullOrEmpty(EasyAuthUserName))
            {
                return EasyAuthUserName;
            }

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
            // EasyAuth principal name is often the email
            if (HasEasyAuthHeaders && !string.IsNullOrEmpty(EasyAuthUserName) && EasyAuthUserName.Contains('@'))
            {
                return EasyAuthUserName;
            }

            return User?.FindFirst("preferred_username")?.Value
                ?? User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.FindFirst("email")?.Value;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            // Check EasyAuth headers first
            if (HasEasyAuthHeaders)
            {
                return true;
            }

            // Fall back to ClaimsPrincipal
            return User?.Identity?.IsAuthenticated == true;
        }
    }

    /// <inheritdoc />
    public bool IsUserIdentity
    {
        get
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            // EasyAuth with AAD IDP is typically a user identity
            if (HasEasyAuthHeaders)
            {
                return string.Equals(EasyAuthIdp, "aad", StringComparison.OrdinalIgnoreCase);
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

            // EasyAuth headers are never from managed identity (those come via bearer tokens)
            if (HasEasyAuthHeaders)
            {
                return false;
            }

            // Check the 'idtyp' claim - if it's 'app', this is a managed identity token
            var idType = User?.FindFirst("idtyp")?.Value;
            return string.Equals(idType, "app", StringComparison.OrdinalIgnoreCase);
        }
    }
}
