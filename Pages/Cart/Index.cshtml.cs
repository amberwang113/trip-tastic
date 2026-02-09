using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Pages.Cart;

public class IndexModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IBookingService _bookingService;
    private readonly IUserContext _userContext;

    public IndexModel(ICartService cartService, IBookingService bookingService, IUserContext userContext)
    {
        _cartService = cartService;
        _bookingService = bookingService;
        _userContext = userContext;
    }

    public Models.Cart Cart { get; private set; } = new();
    public string UserName => _userContext.UserName;
    public bool IsAuthenticated => _userContext.IsAuthenticated;

    public void OnGet()
    {
        Cart = _cartService.GetCart(_userContext.UserId);
    }

    public IActionResult OnPostRemoveFlight(Guid flightId)
    {
        _cartService.RemoveFlight(_userContext.UserId, flightId);
        return RedirectToPage();
    }

    public IActionResult OnPostRemoveHotel(Guid hotelId)
    {
        _cartService.RemoveHotel(_userContext.UserId, hotelId);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        _cartService.ClearCart(_userContext.UserId);
        return RedirectToPage();
    }

    public IActionResult OnPostCheckout()
    {
        var cart = _cartService.GetCart(_userContext.UserId);
        if (cart.TotalItems == 0)
        {
            return RedirectToPage();
        }

        var bookedTrip = _bookingService.Checkout(cart, _userContext.UserId, _userContext.UserName);
        _cartService.ClearCart(_userContext.UserId);

        return RedirectToPage("/MyTrips/Index", new { newBooking = bookedTrip.Id });
    }
}
