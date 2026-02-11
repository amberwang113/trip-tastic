namespace trip_tastic.Services;

/// <summary>
/// Development-only user context that allows switching between simulated users.
/// Uses a cookie to persist the selected user across requests.
/// </summary>
public class DevUserContext : IUserContext
{
    private const string DevUserCookieName = "DevUser";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContext _realUserContext;

    /// <summary>
    /// Available demo users for testing multi-user scenarios.
    /// </summary>
    public static readonly IReadOnlyList<DevUser> AvailableUsers = new List<DevUser>
    {
        new("anonymous", "Anonymous", null),
        new("user-alice-001", "Alice Johnson", "alice@example.com"),
        new("user-bob-002", "Bob Smith", "bob@example.com"),
        new("user-carol-003", "Carol Williams", "carol@example.com"),
    };

    public DevUserContext(IHttpContextAccessor httpContextAccessor, UserContext realUserContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _realUserContext = realUserContext;
    }

    private DevUser? GetSelectedDevUser()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[DevUserCookieName];
        if (string.IsNullOrEmpty(cookie))
        {
            return null;
        }

        return AvailableUsers.FirstOrDefault(u => u.Id == cookie);
    }

    public string UserId
    {
        get
        {
            // If real auth is present, use it
            if (_realUserContext.IsAuthenticated)
            {
                return _realUserContext.UserId;
            }

            // Otherwise use the dev user selection
            return GetSelectedDevUser()?.Id ?? "anonymous";
        }
    }

    public string UserName
    {
        get
        {
            if (_realUserContext.IsAuthenticated)
            {
                return _realUserContext.UserName;
            }

            return GetSelectedDevUser()?.Name ?? "Anonymous";
        }
    }

    public string? UserEmail
    {
        get
        {
            if (_realUserContext.IsAuthenticated)
            {
                return _realUserContext.UserEmail;
            }

            return GetSelectedDevUser()?.Email;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            if (_realUserContext.IsAuthenticated)
            {
                return true;
            }

            // In dev mode, consider non-anonymous users as "authenticated"
            var devUser = GetSelectedDevUser();
            return devUser != null && devUser.Id != "anonymous";
        }
    }

    public bool IsUserIdentity
    {
        get
        {
            if (_realUserContext.IsAuthenticated)
            {
                return _realUserContext.IsUserIdentity;
            }

            // Dev users are simulated user identities
            return IsAuthenticated;
        }
    }

    public bool IsManagedIdentity => _realUserContext.IsManagedIdentity;
}

/// <summary>
/// Represents a simulated user for development testing.
/// </summary>
public record DevUser(string Id, string Name, string? Email);
