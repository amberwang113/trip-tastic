using System.Text.Json.Serialization;

namespace trip_tastic.Models;

public record Hotel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Location { get; init; }
    public required string Address { get; init; }
    public required int StarRating { get; init; }
    public required decimal PricePerNight { get; init; }
    public required int AvailableRooms { get; init; }
    public required IReadOnlyList<string> Amenities { get; init; }
    
    [JsonIgnore]
    public string? ImageUrl { get; init; }
}

public record HotelSearchRequest
{
    public required string Location { get; init; }
    public required DateOnly CheckInDate { get; init; }
    public required DateOnly CheckOutDate { get; init; }
    public int Guests { get; init; } = 1;
    public int Rooms { get; init; } = 1;
}

public record HotelSearchResponse
{
    public required IReadOnlyList<HotelAvailability> Hotels { get; init; }
    public required int TotalResults { get; init; }
}

public record HotelAvailability
{
    public required Hotel Hotel { get; init; }
    public required decimal TotalPrice { get; init; }
    public required int Nights { get; init; }
}

public record HotelBookingRequest
{
    public required Guid HotelId { get; init; }
    public required string GuestName { get; init; }
    public required string GuestEmail { get; init; }
    public required DateOnly CheckInDate { get; init; }
    public required DateOnly CheckOutDate { get; init; }
    public required int NumberOfRooms { get; init; }
    public required int NumberOfGuests { get; init; }
}

public record HotelBooking
{
    public Guid BookingId { get; init; } = Guid.NewGuid();
    public required Guid HotelId { get; init; }
    public required string HotelName { get; init; }
    public required string GuestName { get; init; }
    public required string GuestEmail { get; init; }
    public required DateOnly CheckInDate { get; init; }
    public required DateOnly CheckOutDate { get; init; }
    public required int NumberOfRooms { get; init; }
    public required int NumberOfGuests { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string ConfirmationCode { get; init; }
    public DateTime BookingDate { get; init; } = DateTime.UtcNow;
}
