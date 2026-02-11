using Microsoft.AspNetCore.Mvc;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

/// <summary>
/// Debug controller to inspect authentication state.
/// Remove this in production if not needed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DebugAuthController : ControllerBase
{
    private readonly IUserContext _userContext;

    public DebugAuthController(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Returns current authentication state and relevant headers for debugging.
    /// </summary>
    [HttpGet]
    public ActionResult GetAuthDebugInfo()
    {
        // Get EasyAuth headers
        var easyAuthHeaders = new Dictionary<string, string?>();
        var headerNames = new[]
        {
            "X-MS-CLIENT-PRINCIPAL-ID",
            "X-MS-CLIENT-PRINCIPAL-NAME", 
            "X-MS-CLIENT-PRINCIPAL-IDP",
            "X-MS-CLIENT-PRINCIPAL",
            "X-MS-TOKEN-AAD-ACCESS-TOKEN",
            "X-MS-TOKEN-AAD-ID-TOKEN"
        };

        foreach (var header in headerNames)
        {
            var value = Request.Headers[header].FirstOrDefault();
            easyAuthHeaders[header] = string.IsNullOrEmpty(value) 
                ? null 
                : (header.Contains("TOKEN") ? "[PRESENT - REDACTED]" : value);
        }

        // Get claims from HttpContext.User
        var claims = HttpContext.User?.Claims?
            .Select(c => new { c.Type, c.Value })
            .ToList() ?? [];

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
            EasyAuthHeaders = easyAuthHeaders,
            Claims = claims.Take(20) // Limit to first 20 claims
        });
    }
}
