namespace trip_tastic.Models;

/// <summary>
/// Defines the application roles used for RBAC.
/// These must match the App Roles configured in the Entra ID App Registration.
/// 
/// To configure in Azure Portal:
///   1. Go to Entra ID → App registrations → your app
///   2. Navigate to "App roles" blade
///   3. Create roles with these exact Value strings
///   4. Assign roles to users/groups via Enterprise Applications → Users and groups
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// Full access: manage all resources, access admin/debug endpoints, view all user data.
    /// </summary>
    public const string Admin = "TripTastic.Admin";

    /// <summary>
    /// Standard user: search flights/hotels, manage own cart, book and manage own trips.
    /// </summary>
    public const string User = "TripTastic.User";

    /// <summary>
    /// Read-only: browse flights, hotels, and destinations. Cannot book or modify cart.
    /// </summary>
    public const string Reader = "TripTastic.Reader";
}

/// <summary>
/// Authorization policy names referenced by [Authorize(Policy = "...")] attributes.
/// </summary>
public static class AuthPolicies
{
    /// <summary>Requires TripTastic.Admin role.</summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>Requires TripTastic.User or TripTastic.Admin role.</summary>
    public const string RequireUser = "RequireUser";

    /// <summary>Requires TripTastic.Reader, TripTastic.User, or TripTastic.Admin role.</summary>
    public const string RequireReader = "RequireReader";

    /// <summary>Requires any authenticated user (no specific role needed).</summary>
    public const string RequireAuthenticated = "RequireAuthenticated";
}
