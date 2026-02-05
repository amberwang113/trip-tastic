namespace trip_tastic.Models;

public class BookedTrip
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ConfirmationCode { get; init; }
    public required IReadOnlyList<BookedFlight> Flights { get; init; }
    public required IReadOnlyList<BookedHotel> Hotels { get; init; }
    public required decimal TotalPrice { get; init; }
    public DateTime BookedAt { get; init; } = DateTime.UtcNow;
    public TripStatus Status { get; set; } = TripStatus.Confirmed;
}

public class BookedFlight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Airline { get; init; }
    public required string FlightNumber { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateTime DepartureTime { get; init; }
    public required DateTime ArrivalTime { get; init; }
    public required int Passengers { get; init; }
    public required decimal PricePerSeat { get; init; }
    public decimal TotalPrice => PricePerSeat * Passengers;
    public required string ConfirmationCode { get; init; }
}

public class BookedHotel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string HotelName { get; init; }
    public required string Location { get; init; }
    public required int StarRating { get; init; }
    public required DateOnly CheckInDate { get; init; }
    public required DateOnly CheckOutDate { get; init; }
    public required int Rooms { get; init; }
    public required int Guests { get; init; }
    public required decimal PricePerNight { get; init; }
    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
    public decimal TotalPrice => PricePerNight * Nights * Rooms;
    public required string ConfirmationCode { get; init; }
}

public enum TripStatus
{
    Confirmed,
    Upcoming,
    InProgress,
    Completed,
    Cancelled
}
