using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

/// <summary>
/// Debug controller to inspect authentication state.
/// Restricted to Admin role in production.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public class DebugAuthController : ControllerBase
{
    private readonly IUserContext _userContext;

    public DebugAuthController(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Returns current authentication state and claims for debugging.
    /// </summary>
    [HttpGet]
    public ActionResult GetAuthDebugInfo()
    {
        // Get claims from HttpContext.User
        var claims = HttpContext.User?.Claims?
            .Select(c => new { c.Type, c.Value })
            .ToList() ?? [];

        // Check for Authorization header (redacted)
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        string? authHeaderInfo = null;
        if (!string.IsNullOrEmpty(authHeader))
        {
            authHeaderInfo = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? "Bearer [REDACTED]" : "[PRESENT - REDACTED]";
        }

        return Ok(new
        {
            UserContext = new
            {
                _userContext.UserId,
                _userContext.UserName,
                _userContext.UserEmail,
                _userContext.IsAuthenticated,
                _userContext.IsUserIdentity,
                _userContext.IsManagedIdentity
            },
            HttpContextUser = new
            {
                IsAuthenticated = HttpContext.User?.Identity?.IsAuthenticated ?? false,
                Name = HttpContext.User?.Identity?.Name,
                AuthenticationType = HttpContext.User?.Identity?.AuthenticationType,
                ClaimsCount = claims.Count
            },
            AuthorizationHeader = authHeaderInfo,
            Claims = claims.Take(20) // Limit to first 20 claims
        });
    }
}
