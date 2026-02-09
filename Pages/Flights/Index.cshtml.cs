using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Pages.Flights;

public class IndexModel : PageModel
{
    private readonly IFlightService _flightService;
    private readonly ICartService _cartService;
    private readonly IUserContext _userContext;

    public IndexModel(IFlightService flightService, ICartService cartService, IUserContext userContext)
    {
        _flightService = flightService;
        _cartService = cartService;
        _userContext = userContext;
    }

    public string? SuccessMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Origin { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Destination { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DepartureDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Passengers { get; set; } = 1;

    public FlightSearchResponse? SearchResults { get; private set; }

    public int GetReservedSeatsForFlight(Guid flightId) => _cartService.GetReservedSeatsForFlight(flightId);

    public IReadOnlyList<string> AvailableAirports => DestinationData.AirportCodes;

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Origin) && 
            !string.IsNullOrWhiteSpace(Destination) && 
            DepartureDate.HasValue)
        {
            var request = new FlightSearchRequest
            {
                Origin = Origin,
                Destination = Destination,
                DepartureDate = DepartureDate.Value,
                Passengers = Passengers
            };

            SearchResults = await _flightService.SearchFlightsAsync(request);
        }
    }

    public async Task<IActionResult> OnPostAddToCartAsync(Guid flightId, int passengers)
    {
        var flight = await _flightService.GetFlightByIdAsync(flightId);
        if (flight is null)
        {
            return NotFound();
        }

        _cartService.AddFlight(_userContext.UserId, flight, passengers);
        
        TempData["SuccessMessage"] = $"Added {flight.FlightNumber} to cart!";
        
        // Preserve search parameters
        return RedirectToPage(new { Origin, Destination, DepartureDate, Passengers });
    }
}
