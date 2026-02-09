using System.Security.Claims;

namespace trip_tastic.Services;

/// <summary>
/// Provides access to the current user's identity from JWT token claims.
/// Supports three scenarios:
/// 1. Anonymous (no auth) - returns a default anonymous user
/// 2. Managed Identity (app token) - returns the app's identity
/// 3. OBO (user token) - returns the actual user's identity
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier for the current user.
    /// This is the 'oid' claim from the JWT token, or a default value for anonymous users.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the display name of the current user.
    /// This is the 'name' claim from the JWT token, or "Anonymous" for unauthenticated users.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Gets the email/username of the current user.
    /// This is the 'preferred_username' claim from the JWT token.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Indicates whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Indicates whether the current request is from an OBO (user impersonation) flow.
    /// When true, the identity represents an actual user. When false, it's either anonymous or a managed identity.
    /// </summary>
    bool IsUserIdentity { get; }

    /// <summary>
    /// Indicates whether the current request is from a managed identity (app token).
    /// </summary>
    bool IsManagedIdentity { get; }
}
