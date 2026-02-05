using Microsoft.AspNetCore.Mvc;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    /// <summary>
    /// Search for available hotels
    /// </summary>
    /// <param name="location">City or location name</param>
    /// <param name="checkInDate">Check-in date (YYYY-MM-DD)</param>
    /// <param name="checkOutDate">Check-out date (YYYY-MM-DD)</param>
    /// <param name="guests">Number of guests (default: 1)</param>
    /// <param name="rooms">Number of rooms (default: 1)</param>
    /// <returns>List of available hotels with pricing</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(HotelSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HotelSearchResponse>> SearchHotels(
        [FromQuery] string location,
        [FromQuery] DateOnly checkInDate,
        [FromQuery] DateOnly checkOutDate,
        [FromQuery] int guests = 1,
        [FromQuery] int rooms = 1)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return BadRequest("Location is required");
        }

        if (checkOutDate <= checkInDate)
        {
            return BadRequest("Check-out date must be after check-in date");
        }

        if (guests < 1 || rooms < 1)
        {
            return BadRequest("At least one guest and one room are required");
        }

        var request = new HotelSearchRequest
        {
            Location = location,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Guests = guests,
            Rooms = rooms
        };

        var result = await _hotelService.SearchHotelsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get hotel details by ID
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <returns>Hotel details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Hotel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Hotel>> GetHotel(Guid id)
    {
        var hotel = await _hotelService.GetHotelByIdAsync(id);
        if (hotel is null)
        {
            return NotFound();
        }
        return Ok(hotel);
    }

    /// <summary>
    /// Book a hotel
    /// </summary>
    /// <param name="request">Booking details</param>
    /// <returns>Booking confirmation</returns>
    [HttpPost("book")]
    [ProducesResponseType(typeof(HotelBooking), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelBooking>> BookHotel([FromBody] HotelBookingRequest request)
    {
        try
        {
            var booking = await _hotelService.BookHotelAsync(request);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.BookingId }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get booking details by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    [HttpGet("bookings/{id:guid}")]
    [ProducesResponseType(typeof(HotelBooking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelBooking>> GetBooking(Guid id)
    {
        var booking = await _hotelService.GetBookingAsync(id);
        if (booking is null)
        {
            return NotFound();
        }
        return Ok(booking);
    }
}
