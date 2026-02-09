using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Pages.Hotels;

public class IndexModel : PageModel
{
    private readonly IHotelService _hotelService;
    private readonly ICartService _cartService;
    private readonly IUserContext _userContext;

    public IndexModel(IHotelService hotelService, ICartService cartService, IUserContext userContext)
    {
        _hotelService = hotelService;
        _cartService = cartService;
        _userContext = userContext;
    }

    [BindProperty(SupportsGet = true)]
    public string? Location { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? CheckInDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? CheckOutDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Guests { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int Rooms { get; set; } = 1;

    public HotelSearchResponse? SearchResults { get; private set; }

    public int GetReservedRoomsForHotel(Guid hotelId) => _cartService.GetReservedRoomsForHotel(hotelId);

    public IReadOnlyList<string> AvailableLocations => DestinationData.CityNames;

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Location) && 
            CheckInDate.HasValue && 
            CheckOutDate.HasValue &&
            CheckOutDate > CheckInDate)
        {
            var request = new HotelSearchRequest
            {
                Location = Location,
                CheckInDate = CheckInDate.Value,
                CheckOutDate = CheckOutDate.Value,
                Guests = Guests,
                Rooms = Rooms
            };

            SearchResults = await _hotelService.SearchHotelsAsync(request);
        }
    }

    public async Task<IActionResult> OnPostAddToCartAsync(
        Guid hotelId, 
        DateOnly checkInDate, 
        DateOnly checkOutDate, 
        int rooms, 
        int guests)
    {
        var hotel = await _hotelService.GetHotelByIdAsync(hotelId);
        if (hotel is null)
        {
            return NotFound();
        }

        _cartService.AddHotel(_userContext.UserId, hotel, checkInDate, checkOutDate, rooms, guests);
        
        TempData["SuccessMessage"] = $"Added {hotel.Name} to cart!";
        
        // Preserve search parameters
        return RedirectToPage(new { Location, CheckInDate = checkInDate, CheckOutDate = checkOutDate, Rooms = rooms, Guests = guests });
    }
}
