using Microsoft.AspNetCore.Mvc;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightsController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    /// <summary>
    /// Search for available flights
    /// </summary>
    /// <param name="origin">Origin airport code (e.g., JFK)</param>
    /// <param name="destination">Destination airport code (e.g., LAX)</param>
    /// <param name="departureDate">Departure date (YYYY-MM-DD)</param>
    /// <param name="passengers">Number of passengers (default: 1)</param>
    /// <returns>List of available flights</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(FlightSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FlightSearchResponse>> SearchFlights(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] DateOnly departureDate,
        [FromQuery] int passengers = 1)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest("Origin and destination are required");
        }

        if (passengers < 1)
        {
            return BadRequest("At least one passenger is required");
        }

        var request = new FlightSearchRequest
        {
            Origin = origin,
            Destination = destination,
            DepartureDate = departureDate,
            Passengers = passengers
        };

        var result = await _flightService.SearchFlightsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get flight details by ID
    /// </summary>
    /// <param name="id">Flight ID</param>
    /// <returns>Flight details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Flight), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Flight>> GetFlight(Guid id)
    {
        var flight = await _flightService.GetFlightByIdAsync(id);
        if (flight is null)
        {
            return NotFound();
        }
        return Ok(flight);
    }

    /// <summary>
    /// Book a flight
    /// </summary>
    /// <param name="request">Booking details</param>
    /// <returns>Booking confirmation</returns>
    [HttpPost("book")]
    [ProducesResponseType(typeof(FlightBooking), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlightBooking>> BookFlight([FromBody] FlightBookingRequest request)
    {
        try
        {
            var booking = await _flightService.BookFlightAsync(request);
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
    [ProducesResponseType(typeof(FlightBooking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlightBooking>> GetBooking(Guid id)
    {
        var booking = await _flightService.GetBookingAsync(id);
        if (booking is null)
        {
            return NotFound();
        }
        return Ok(booking);
    }
}
