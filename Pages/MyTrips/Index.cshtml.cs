using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Pages.MyTrips;

public class IndexModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly IUserContext _userContext;

    public IndexModel(IBookingService bookingService, IUserContext userContext)
    {
        _bookingService = bookingService;
        _userContext = userContext;
    }

    public IReadOnlyList<BookedTrip> Trips { get; private set; } = [];
    public Guid? NewBookingId { get; private set; }
    public string UserName => _userContext.UserName;
    public bool IsAuthenticated => _userContext.IsAuthenticated;

    public void OnGet(Guid? newBooking = null)
    {
        Trips = _bookingService.GetTripsForUser(_userContext.UserId);
        NewBookingId = newBooking;
    }

    public IActionResult OnPostCancel(Guid tripId)
    {
        _bookingService.CancelTrip(tripId, _userContext.UserId);
        return RedirectToPage();
    }
}
