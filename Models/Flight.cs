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
