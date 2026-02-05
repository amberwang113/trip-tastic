using trip_tastic.Models;

namespace trip_tastic.Services;

public class HotelService : IHotelService
{
    private static readonly string[] HotelNames = ["Grand Palace Hotel", "Seaside Resort", "Mountain View Lodge", "City Center Inn", "Luxury Suites", "Comfort Stay", "Royal Gardens Hotel", "Sunset Beach Resort"];
    private static readonly IReadOnlyList<string> Locations = DestinationData.CityNames;
    private static readonly string[][] AmenityOptions =
    [
        ["Free WiFi", "Pool", "Gym", "Restaurant", "Bar", "Spa"],
        ["Free WiFi", "Pool", "Gym", "Room Service", "Parking"],
        ["Free WiFi", "Gym", "Business Center", "Restaurant"],
        ["Free WiFi", "Pool", "Beach Access", "Restaurant", "Bar", "Spa", "Tennis Court"],
        ["Free WiFi", "Gym", "Parking", "Pet Friendly"]
    ];

    private readonly List<Hotel> _hotels = [];
    private readonly Dictionary<Guid, HotelBooking> _bookings = [];
    private readonly Random _random = new();

    public HotelService()
    {
        GenerateSampleHotels();
    }

    private void GenerateSampleHotels()
    {
        var isFirstHotel = true;
        foreach (var location in Locations)
        {
            var hotelsInLocation = _random.Next(3, 8);
            for (var i = 0; i < hotelsInLocation; i++)
            {
                var baseName = HotelNames[_random.Next(HotelNames.Length)];
                var starRating = _random.Next(2, 6);
                var amenities = AmenityOptions[_random.Next(AmenityOptions.Length)];

                // Ensure at least one hotel has only 1 room available
                var availableRooms = isFirstHotel ? 1 : _random.Next(5, 50);
                isFirstHotel = false;

                // Use picsum.photos for reliable placeholder images
                var imageId = _random.Next(1, 200);
                _hotels.Add(new Hotel
                {
                    Name = $"{baseName} {location}",
                    Location = location,
                    Address = $"{_random.Next(1, 999)} {GetRandomStreetName()} Street, {location}",
                    StarRating = starRating,
                    PricePerNight = Math.Round((decimal)(starRating * 50 + _random.NextDouble() * 200), 2),
                    AvailableRooms = availableRooms,
                    Amenities = amenities.ToList(),
                    ImageUrl = $"https://picsum.photos/seed/hotel{imageId}/400/300"
                });
            }
        }
    }

    private string GetRandomStreetName()
    {
        string[] streets = ["Main", "Oak", "Park", "Cedar", "Elm", "View", "Lake", "Hill", "River", "Ocean"];
        return streets[_random.Next(streets.Length)];
    }

    public Task<HotelSearchResponse> SearchHotelsAsync(HotelSearchRequest request)
    {
        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        if (nights <= 0)
        {
            return Task.FromResult(new HotelSearchResponse
            {
                Hotels = [],
                TotalResults = 0
            });
        }

        var matchingHotels = _hotels
            .Where(h => h.Location.Equals(request.Location, StringComparison.OrdinalIgnoreCase) &&
                        h.AvailableRooms >= request.Rooms)
            .Select(h => new HotelAvailability
            {
                Hotel = h,
                Nights = nights,
                TotalPrice = h.PricePerNight * nights * request.Rooms
            })
            .OrderBy(h => h.TotalPrice)
            .ToList();

        return Task.FromResult(new HotelSearchResponse
        {
            Hotels = matchingHotels,
            TotalResults = matchingHotels.Count
        });
    }

    public Task<Hotel?> GetHotelByIdAsync(Guid hotelId)
    {
        return Task.FromResult(_hotels.FirstOrDefault(h => h.Id == hotelId));
    }

    public Task<HotelBooking> BookHotelAsync(HotelBookingRequest request)
    {
        var hotel = _hotels.FirstOrDefault(h => h.Id == request.HotelId)
            ?? throw new InvalidOperationException("Hotel not found");

        if (hotel.AvailableRooms < request.NumberOfRooms)
        {
            throw new InvalidOperationException("Not enough rooms available");
        }

        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        if (nights <= 0)
        {
            throw new InvalidOperationException("Invalid date range");
        }

        var booking = new HotelBooking
        {
            HotelId = request.HotelId,
            HotelName = hotel.Name,
            GuestName = request.GuestName,
            GuestEmail = request.GuestEmail,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            NumberOfRooms = request.NumberOfRooms,
            NumberOfGuests = request.NumberOfGuests,
            TotalPrice = hotel.PricePerNight * nights * request.NumberOfRooms,
            ConfirmationCode = GenerateConfirmationCode()
        };

        _bookings[booking.BookingId] = booking;
        return Task.FromResult(booking);
    }

    public Task<HotelBooking?> GetBookingAsync(Guid bookingId)
    {
        return Task.FromResult(_bookings.GetValueOrDefault(bookingId));
    }

    private string GenerateConfirmationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 8).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }
}
