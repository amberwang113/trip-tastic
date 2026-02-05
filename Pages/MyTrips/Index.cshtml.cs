using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Pages.MyTrips;

public class IndexModel : PageModel
{
    private readonly IBookingService _bookingService;

    public IndexModel(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public IReadOnlyList<BookedTrip> Trips { get; private set; } = [];
    public Guid? NewBookingId { get; private set; }

    public void OnGet(Guid? newBooking = null)
    {
        Trips = _bookingService.GetAllTrips();
        NewBookingId = newBooking;
    }

    public IActionResult OnPostCancel(Guid tripId)
    {
        _bookingService.CancelTrip(tripId);
        return RedirectToPage();
    }
}
