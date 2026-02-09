using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IBookingService _bookingService;
    private readonly IFlightService _flightService;
    private readonly IHotelService _hotelService;
    private readonly IUserContext _userContext;

    public CartController(
        ICartService cartService,
        IBookingService bookingService,
        IFlightService flightService,
        IHotelService hotelService,
        IUserContext userContext)
    {
        _cartService = cartService;
        _bookingService = bookingService;
        _flightService = flightService;
        _hotelService = hotelService;
        _userContext = userContext;
    }

    /// <summary>
    /// Get the current shopping cart contents
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public ActionResult<CartResponse> GetCart()
    {
        var cart = _cartService.GetCart(_userContext.UserId);
        return Ok(MapCartToResponse(cart));
    }

    /// <summary>
    /// Add a flight to the shopping cart
    /// </summary>
    [HttpPost("flights")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>> AddFlightToCart([FromBody] AddFlightToCartRequest request)
    {
        if (request.Passengers < 1)
        {
            return BadRequest("At least one passenger is required");
        }

        var flight = await _flightService.GetFlightByIdAsync(request.FlightId);
        if (flight is null)
        {
            return NotFound($"Flight with ID {request.FlightId} not found");
        }

        if (flight.AvailableSeats < request.Passengers)
        {
            return BadRequest($"Not enough seats available. Requested: {request.Passengers}, Available: {flight.AvailableSeats}");
        }

        _cartService.AddFlight(_userContext.UserId, flight, request.Passengers);
        var cart = _cartService.GetCart(_userContext.UserId);
        return Ok(MapCartToResponse(cart));
    }

    /// <summary>
    /// Add a hotel to the shopping cart
    /// </summary>
    [HttpPost("hotels")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>> AddHotelToCart([FromBody] AddHotelToCartRequest request)
    {
        if (request.Rooms < 1)
        {
            return BadRequest("At least one room is required");
        }

        if (request.Guests < 1)
        {
            return BadRequest("At least one guest is required");
        }

        if (request.CheckOutDate <= request.CheckInDate)
        {
            return BadRequest("Check-out date must be after check-in date");
        }

        var hotel = await _hotelService.GetHotelByIdAsync(request.HotelId);
        if (hotel is null)
        {
            return NotFound($"Hotel with ID {request.HotelId} not found");
        }

        if (hotel.AvailableRooms < request.Rooms)
        {
            return BadRequest($"Not enough rooms available. Requested: {request.Rooms}, Available: {hotel.AvailableRooms}");
        }

        _cartService.AddHotel(_userContext.UserId, hotel, request.CheckInDate, request.CheckOutDate, request.Rooms, request.Guests);
        var cart = _cartService.GetCart(_userContext.UserId);
        return Ok(MapCartToResponse(cart));
    }

    /// <summary>
    /// Remove a flight from the shopping cart
    /// </summary>
    [HttpDelete("flights/{flightId:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public ActionResult<CartResponse> RemoveFlightFromCart(Guid flightId)
    {
        _cartService.RemoveFlight(_userContext.UserId, flightId);
        var cart = _cartService.GetCart(_userContext.UserId);
        return Ok(MapCartToResponse(cart));
    }

    /// <summary>
    /// Remove a hotel from the shopping cart
    /// </summary>
    [HttpDelete("hotels/{hotelId:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public ActionResult<CartResponse> RemoveHotelFromCart(Guid hotelId)
    {
        _cartService.RemoveHotel(_userContext.UserId, hotelId);
        var cart = _cartService.GetCart(_userContext.UserId);
        return Ok(MapCartToResponse(cart));
    }

    /// <summary>
    /// Clear all items from the shopping cart
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public ActionResult<CartResponse> ClearCart()
    {
        _cartService.ClearCart(_userContext.UserId);
        var cart = _cartService.GetCart(_userContext.UserId);
        return Ok(MapCartToResponse(cart));
    }

    /// <summary>
    /// Checkout the cart and create a booked trip
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CheckoutResponse> Checkout()
    {
        var cart = _cartService.GetCart(_userContext.UserId);
        
        if (cart.Items.Count == 0)
        {
            return BadRequest("Cart is empty. Add flights or hotels before checking out.");
        }

        var bookedTrip = _bookingService.Checkout(cart, _userContext.UserId, _userContext.UserName);
        _cartService.ClearCart(_userContext.UserId);

        return Ok(new CheckoutResponse
        {
            Success = true,
            Message = "Checkout successful! Your trip has been booked.",
            BookedTrip = MapBookedTripToResponse(bookedTrip)
        });
    }

    /// <summary>
    /// Get all booked trips for the current user
    /// </summary>
    [HttpGet("trips")]
    [ProducesResponseType(typeof(BookedTripsResponse), StatusCodes.Status200OK)]
    public ActionResult<BookedTripsResponse> GetAllTrips()
    {
        var trips = _bookingService.GetTripsForUser(_userContext.UserId);
        return Ok(new BookedTripsResponse
        {
            Trips = trips.Select(MapBookedTripToResponse).ToList(),
            TotalTrips = trips.Count
        });
    }

    /// <summary>
    /// Get a specific booked trip by ID (must belong to current user)
    /// </summary>
    [HttpGet("trips/{tripId:guid}")]
    [ProducesResponseType(typeof(BookedTripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BookedTripResponse> GetTrip(Guid tripId)
    {
        var trip = _bookingService.GetTrip(tripId, _userContext.UserId);
        if (trip is null)
        {
            return NotFound($"Trip with ID {tripId} not found");
        }
        return Ok(MapBookedTripToResponse(trip));
    }

    /// <summary>
    /// Cancel a booked trip (must belong to current user)
    /// </summary>
    [HttpPost("trips/{tripId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelTripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CancelTripResponse> CancelTrip(Guid tripId)
    {
        var success = _bookingService.CancelTrip(tripId, _userContext.UserId);
        if (!success)
        {
            return NotFound($"Trip with ID {tripId} not found");
        }

        return Ok(new CancelTripResponse
        {
            Success = true,
            Message = "Trip has been cancelled successfully.",
            TripId = tripId
        });
    }

    private static CartResponse MapCartToResponse(Cart cart)
    {
        return new CartResponse
        {
            Items = cart.Items.Select(item => new CartItemResponse
            {
                Id = item.Id,
                Type = item.Type.ToString().ToLowerInvariant(),
                Description = item.Description,
                TotalPrice = item.TotalPrice,
                Flight = item.Flight is not null ? new CartFlightInfo
                {
                    Id = item.Flight.Id,
                    Airline = item.Flight.Airline,
                    FlightNumber = item.Flight.FlightNumber,
                    Origin = item.Flight.Origin,
                    Destination = item.Flight.Destination,
                    DepartureTime = item.Flight.DepartureTime,
                    ArrivalTime = item.Flight.ArrivalTime,
                    PricePerSeat = item.Flight.Price,
                    Passengers = item.Passengers
                } : null,
                Hotel = item.Hotel is not null ? new CartHotelInfo
                {
                    Id = item.Hotel.Id,
                    Name = item.Hotel.Name,
                    Location = item.Hotel.Location,
                    StarRating = item.Hotel.StarRating,
                    PricePerNight = item.Hotel.PricePerNight,
                    CheckInDate = item.CheckInDate?.ToString("yyyy-MM-dd") ?? "",
                    CheckOutDate = item.CheckOutDate?.ToString("yyyy-MM-dd") ?? "",
                    Nights = item.Nights,
                    Rooms = item.Rooms,
                    Guests = item.Guests
                } : null
            }).ToList(),
            TotalItems = cart.TotalItems,
            TotalPrice = cart.TotalPrice
        };
    }

    private static BookedTripResponse MapBookedTripToResponse(BookedTrip trip)
    {
        return new BookedTripResponse
        {
            Id = trip.Id,
            ConfirmationCode = trip.ConfirmationCode,
            Status = trip.Status.ToString(),
            BookedAt = trip.BookedAt,
            TotalPrice = trip.TotalPrice,
            Flights = trip.Flights.Select(f => new BookedFlightResponse
            {
                Id = f.Id,
                ConfirmationCode = f.ConfirmationCode,
                Airline = f.Airline,
                FlightNumber = f.FlightNumber,
                Origin = f.Origin,
                Destination = f.Destination,
                DepartureTime = f.DepartureTime,
                ArrivalTime = f.ArrivalTime,
                Passengers = f.Passengers,
                PricePerSeat = f.PricePerSeat,
                TotalPrice = f.TotalPrice
            }).ToList(),
            Hotels = trip.Hotels.Select(h => new BookedHotelResponse
            {
                Id = h.Id,
                ConfirmationCode = h.ConfirmationCode,
                HotelName = h.HotelName,
                Location = h.Location,
                StarRating = h.StarRating,
                CheckInDate = h.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = h.CheckOutDate.ToString("yyyy-MM-dd"),
                Nights = h.Nights,
                Rooms = h.Rooms,
                Guests = h.Guests,
                PricePerNight = h.PricePerNight,
                TotalPrice = h.TotalPrice
            }).ToList()
        };
    }
}

#region Request/Response Models

public record AddFlightToCartRequest
{
    [JsonPropertyName("flightId")]
    public Guid FlightId { get; init; }

    [JsonPropertyName("passengers")]
    public int Passengers { get; init; } = 1;
}

public record AddHotelToCartRequest
{
    [JsonPropertyName("hotelId")]
    public Guid HotelId { get; init; }

    [JsonPropertyName("checkInDate")]
    public DateOnly CheckInDate { get; init; }

    [JsonPropertyName("checkOutDate")]
    public DateOnly CheckOutDate { get; init; }

    [JsonPropertyName("rooms")]
    public int Rooms { get; init; } = 1;

    [JsonPropertyName("guests")]
    public int Guests { get; init; } = 1;
}

public record CartResponse
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<CartItemResponse> Items { get; init; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; init; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; init; }
}

public record CartItemResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; init; }

    [JsonPropertyName("flight")]
    public CartFlightInfo? Flight { get; init; }

    [JsonPropertyName("hotel")]
    public CartHotelInfo? Hotel { get; init; }
}

public record CartFlightInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("airline")]
    public required string Airline { get; init; }

    [JsonPropertyName("flightNumber")]
    public required string FlightNumber { get; init; }

    [JsonPropertyName("origin")]
    public required string Origin { get; init; }

    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    [JsonPropertyName("departureTime")]
    public DateTime DepartureTime { get; init; }

    [JsonPropertyName("arrivalTime")]
    public DateTime ArrivalTime { get; init; }

    [JsonPropertyName("pricePerSeat")]
    public decimal PricePerSeat { get; init; }

    [JsonPropertyName("passengers")]
    public int Passengers { get; init; }
}

public record CartHotelInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("location")]
    public required string Location { get; init; }

    [JsonPropertyName("starRating")]
    public int StarRating { get; init; }

    [JsonPropertyName("pricePerNight")]
    public decimal PricePerNight { get; init; }

    [JsonPropertyName("checkInDate")]
    public required string CheckInDate { get; init; }

    [JsonPropertyName("checkOutDate")]
    public required string CheckOutDate { get; init; }

    [JsonPropertyName("nights")]
    public int Nights { get; init; }

    [JsonPropertyName("rooms")]
    public int Rooms { get; init; }

    [JsonPropertyName("guests")]
    public int Guests { get; init; }
}

public record CheckoutResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("bookedTrip")]
    public required BookedTripResponse BookedTrip { get; init; }
}

public record BookedTripsResponse
{
    [JsonPropertyName("trips")]
    public required IReadOnlyList<BookedTripResponse> Trips { get; init; }

    [JsonPropertyName("totalTrips")]
    public int TotalTrips { get; init; }
}

public record BookedTripResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("confirmationCode")]
    public required string ConfirmationCode { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("bookedAt")]
    public DateTime BookedAt { get; init; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; init; }

    [JsonPropertyName("flights")]
    public required IReadOnlyList<BookedFlightResponse> Flights { get; init; }

    [JsonPropertyName("hotels")]
    public required IReadOnlyList<BookedHotelResponse> Hotels { get; init; }
}

public record BookedFlightResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("confirmationCode")]
    public required string ConfirmationCode { get; init; }

    [JsonPropertyName("airline")]
    public required string Airline { get; init; }

    [JsonPropertyName("flightNumber")]
    public required string FlightNumber { get; init; }

    [JsonPropertyName("origin")]
    public required string Origin { get; init; }

    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    [JsonPropertyName("departureTime")]
    public DateTime DepartureTime { get; init; }

    [JsonPropertyName("arrivalTime")]
    public DateTime ArrivalTime { get; init; }

    [JsonPropertyName("passengers")]
    public int Passengers { get; init; }

    [JsonPropertyName("pricePerSeat")]
    public decimal PricePerSeat { get; init; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; init; }
}

public record BookedHotelResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("confirmationCode")]
    public required string ConfirmationCode { get; init; }

    [JsonPropertyName("hotelName")]
    public required string HotelName { get; init; }

    [JsonPropertyName("location")]
    public required string Location { get; init; }

    [JsonPropertyName("starRating")]
    public int StarRating { get; init; }

    [JsonPropertyName("checkInDate")]
    public required string CheckInDate { get; init; }

    [JsonPropertyName("checkOutDate")]
    public required string CheckOutDate { get; init; }

    [JsonPropertyName("nights")]
    public int Nights { get; init; }

    [JsonPropertyName("rooms")]
    public int Rooms { get; init; }

    [JsonPropertyName("guests")]
    public int Guests { get; init; }

    [JsonPropertyName("pricePerNight")]
    public decimal PricePerNight { get; init; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; init; }
}

public record CancelTripResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("tripId")]
    public Guid TripId { get; init; }
}

#endregion
