using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Pages.Cart;

public class IndexModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IBookingService _bookingService;

    public IndexModel(ICartService cartService, IBookingService bookingService)
    {
        _cartService = cartService;
        _bookingService = bookingService;
    }

    public Models.Cart Cart { get; private set; } = new();

    public void OnGet()
    {
        Cart = _cartService.GetCart();
    }

    public IActionResult OnPostRemoveFlight(Guid flightId)
    {
        _cartService.RemoveFlight(flightId);
        return RedirectToPage();
    }

    public IActionResult OnPostRemoveHotel(Guid hotelId)
    {
        _cartService.RemoveHotel(hotelId);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        _cartService.ClearCart();
        return RedirectToPage();
    }

    public IActionResult OnPostCheckout()
    {
        var cart = _cartService.GetCart();
        if (cart.TotalItems == 0)
        {
            return RedirectToPage();
        }

        var bookedTrip = _bookingService.Checkout(cart);
        _cartService.ClearCart();

        return RedirectToPage("/MyTrips/Index", new { newBooking = bookedTrip.Id });
    }
}
