namespace trip_tastic.Models;

public record Flight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Airline { get; init; }
    public required string FlightNumber { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateTime DepartureTime { get; init; }
    public required DateTime ArrivalTime { get; init; }
    public required decimal Price { get; init; }
    public required int AvailableSeats { get; init; }
}

public record FlightSearchRequest
{
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public int Passengers { get; init; } = 1;
}

public record FlightSearchResponse
{
    public required IReadOnlyList<Flight> Flights { get; init; }
    public required int TotalResults { get; init; }
}

public record FlightBookingRequest
{
    public required Guid FlightId { get; init; }
    public required string PassengerName { get; init; }
    public required string PassengerEmail { get; init; }
    public required int NumberOfSeats { get; init; }
}

public record FlightBooking
{
    public Guid BookingId { get; init; } = Guid.NewGuid();
    public required Guid FlightId { get; init; }
    public required string PassengerName { get; init; }
    public required string PassengerEmail { get; init; }
    public required int NumberOfSeats { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string ConfirmationCode { get; init; }
    public DateTime BookingDate { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request to list flights for a day with optional filters
/// </summary>
public record FlightListRequest
{
    public required DateOnly Date { get; init; }
    public string? Origin { get; init; }
    public string? Destination { get; init; }
    public string? Airline { get; init; }
    public int MinSeats { get; init; } = 1;
    public decimal? MaxPrice { get; init; }
    public string SortBy { get; init; } = "departure";
    public int Limit { get; init; } = 50;
}

/// <summary>
/// Response containing a list of flights with metadata
/// </summary>
public record FlightListResponse
{
    public required IReadOnlyList<Flight> Flights { get; init; }
    public required int TotalResults { get; init; }
    public required int ReturnedResults { get; init; }
    public required DateOnly Date { get; init; }
    public required FlightListFilters AppliedFilters { get; init; }
}

/// <summary>
/// Filters that were applied to the flight list
/// </summary>
public record FlightListFilters
{
    public string? Origin { get; init; }
    public string? Destination { get; init; }
    public string? Airline { get; init; }
    public int MinSeats { get; init; }
    public decimal? MaxPrice { get; init; }
    public string SortBy { get; init; } = "departure";
}

/// <summary>
/// Information about an available airport
/// </summary>
public record AirportInfo
{
    public required string Code { get; init; }
    public required string City { get; init; }
}
