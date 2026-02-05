using trip_tastic.Models;

namespace trip_tastic.Services;

public class BookingService : IBookingService
{
    private readonly List<BookedTrip> _bookedTrips = [];
    private static readonly Random _random = new();

    public BookedTrip Checkout(Cart cart)
    {
        var bookedFlights = new List<BookedFlight>();
        var bookedHotels = new List<BookedHotel>();

        foreach (var item in cart.Items)
        {
            if (item.Type == CartItemType.Flight && item.Flight is not null)
            {
                bookedFlights.Add(new BookedFlight
                {
                    Airline = item.Flight.Airline,
                    FlightNumber = item.Flight.FlightNumber,
                    Origin = item.Flight.Origin,
                    Destination = item.Flight.Destination,
                    DepartureTime = item.Flight.DepartureTime,
                    ArrivalTime = item.Flight.ArrivalTime,
                    Passengers = item.Passengers,
                    PricePerSeat = item.Flight.Price,
                    ConfirmationCode = GenerateConfirmationCode("FL")
                });
            }
            else if (item.Type == CartItemType.Hotel && item.Hotel is not null)
            {
                bookedHotels.Add(new BookedHotel
                {
                    HotelName = item.Hotel.Name,
                    Location = item.Hotel.Location,
                    StarRating = item.Hotel.StarRating,
                    CheckInDate = item.CheckInDate!.Value,
                    CheckOutDate = item.CheckOutDate!.Value,
                    Rooms = item.Rooms,
                    Guests = item.Guests,
                    PricePerNight = item.Hotel.PricePerNight,
                    ConfirmationCode = GenerateConfirmationCode("HT")
                });
            }
        }

        var trip = new BookedTrip
        {
            ConfirmationCode = GenerateConfirmationCode("TT"),
            Flights = bookedFlights,
            Hotels = bookedHotels,
            TotalPrice = cart.TotalPrice,
            Status = TripStatus.Confirmed
        };

        _bookedTrips.Add(trip);
        return trip;
    }

    public IReadOnlyList<BookedTrip> GetAllTrips()
    {
        return _bookedTrips.OrderByDescending(t => t.BookedAt).ToList();
    }

    public BookedTrip? GetTrip(Guid tripId)
    {
        return _bookedTrips.FirstOrDefault(t => t.Id == tripId);
    }

    public bool CancelTrip(Guid tripId)
    {
        var trip = _bookedTrips.FirstOrDefault(t => t.Id == tripId);
        if (trip is null)
            return false;

        trip.Status = TripStatus.Cancelled;
        return true;
    }

    public int GetTripCount()
    {
        return _bookedTrips.Count(t => t.Status != TripStatus.Cancelled);
    }

    private static string GenerateConfirmationCode(string prefix)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[6];
        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[_random.Next(chars.Length)];
        }
        return $"{prefix}-{new string(code)}";
    }
}
