using Microsoft.AspNetCore.Mvc;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

/// <summary>
/// Development-only controller for switching between simulated users.
/// This controller is only registered in Development environment.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevAuthController : ControllerBase
{
    private const string DevUserCookieName = "DevUser";

    /// <summary>
    /// Get available demo users.
    /// </summary>
    [HttpGet("users")]
    public ActionResult<IEnumerable<object>> GetUsers()
    {
        return Ok(DevUserContext.AvailableUsers.Select(u => new
        {
            u.Id,
            u.Name,
            u.Email
        }));
    }

    /// <summary>
    /// Switch to a different demo user.
    /// </summary>
    [HttpPost("switch/{userId}")]
    public ActionResult SwitchUser(string userId)
    {
        var user = DevUserContext.AvailableUsers.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return NotFound($"User '{userId}' not found");
        }

        if (userId == "anonymous")
        {
            // Clear the cookie to become anonymous
            Response.Cookies.Delete(DevUserCookieName);
        }
        else
        {
            // Set the cookie to switch users
            Response.Cookies.Append(DevUserCookieName, userId, new CookieOptions
            {
                HttpOnly = false, // Allow JavaScript to read it for UI updates
                Secure = false,   // Allow on localhost HTTP
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7)
            });
        }

        return Ok(new
        {
            Success = true,
            Message = $"Switched to user: {user.Name}",
            User = new { user.Id, user.Name, user.Email }
        });
    }

    /// <summary>
    /// Get the currently selected demo user.
    /// </summary>
    [HttpGet("current")]
    public ActionResult GetCurrentUser()
    {
        var cookie = Request.Cookies[DevUserCookieName];
        var user = DevUserContext.AvailableUsers.FirstOrDefault(u => u.Id == cookie)
                   ?? DevUserContext.AvailableUsers.First(); // Default to anonymous

        return Ok(new { user.Id, user.Name, user.Email });
    }
}
