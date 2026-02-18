using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Services;

namespace trip_tastic.Pages.DebugLog;

public class IndexModel : PageModel
{
    private readonly RequestLogService _logService;
    private readonly IUserContext _userContext;

    public IndexModel(RequestLogService logService, IUserContext userContext)
    {
        _logService = logService;
        _userContext = userContext;
    }

    public IReadOnlyList<RequestLogEntry> Logs { get; private set; } = [];
    public int TotalLogs { get; private set; }
    
    // Current user info for display
    public string CurrentUserId { get; private set; } = "";
    public string CurrentUserName { get; private set; } = "";
    public bool CurrentUserIsAuthenticated { get; private set; }

    public void OnGet(int? limit = 100)
    {
        Logs = _logService.GetLogs(limit);
        TotalLogs = _logService.Count;
        
        CurrentUserId = _userContext.UserId;
        CurrentUserName = _userContext.UserName;
        CurrentUserIsAuthenticated = _userContext.IsAuthenticated;
    }

    public IActionResult OnPostClear()
    {
        _logService.Clear();
        return RedirectToPage();
    }
}
