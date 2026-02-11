using trip_tastic.Models;

namespace trip_tastic.Services;

public class FlightService : IFlightService
{
    private static readonly string[] Airlines = ["TripTastic Airways", "SkyHigh Airlines", "Global Express", "Pacific Wings", "Atlantic Jet"];
    private static readonly IReadOnlyList<string> Airports = DestinationData.AirportCodes;
    
    private readonly List<Flight> _flights = [];
    private readonly Dictionary<Guid, FlightBooking> _bookings = [];
    private Random _random = new(42); // Fixed seed for reproducibility
    private DateOnly _lastGeneratedDate;
    private readonly object _lock = new();

    public FlightService()
    {
        RegenerateFlightsIfNeeded();
    }

    public IEnumerable<AirportInfo> GetAvailableAirports()
    {
        return DestinationData.All.Select(d => new AirportInfo
        {
            Code = d.AirportCode,
            City = d.CityName
        });
    }

    private void RegenerateFlightsIfNeeded()
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        
        lock (_lock)
        {
            // Regenerate flights if it's a new day or flights haven't been generated
            if (_lastGeneratedDate != todayUtc || _flights.Count == 0)
            {
                _flights.Clear();
                _random = new Random(42); // Reset seed for consistent generation
                GenerateSampleFlights(todayUtc);
                _lastGeneratedDate = todayUtc;
            }
        }
    }

    private void GenerateSampleFlights(DateOnly today)
    {
        var isFirstFlight = true;
        
        // Generate flights between all airport combinations
        foreach (var origin in Airports)
        {
            foreach (var destination in Airports.Where(a => a != origin))
            {
                // Generate flights for the next 30 days
                for (var dayOffset = 1; dayOffset <= 30; dayOffset++)
                {
                    var departureDate = today.AddDays(dayOffset);
                    var flightsPerDay = _random.Next(2, 4);

                    for (var i = 0; i < flightsPerDay; i++)
                    {
                        var departureHour = _random.Next(6, 22);
                        var flightDuration = _random.Next(2, 12);
                        var airline = Airlines[_random.Next(Airlines.Length)];

                        // Ensure at least one flight has only 1 seat available
                        var availableSeats = isFirstFlight ? 1 : _random.Next(5, 180);
                        isFirstFlight = false;

                        // Use UTC for all times
                        var departureDateTime = departureDate.ToDateTime(new TimeOnly(departureHour, _random.Next(0, 4) * 15), DateTimeKind.Utc);
                        var arrivalDateTime = departureDateTime.AddHours(flightDuration).AddMinutes(_random.Next(0, 4) * 15);

                        _flights.Add(new Flight
                        {
                            Airline = airline,
                            FlightNumber = $"{airline[..2].ToUpper()}{_random.Next(100, 9999)}",
                            Origin = origin,
                            Destination = destination,
                            DepartureTime = departureDateTime,
                            ArrivalTime = arrivalDateTime,
                            Price = Math.Round((decimal)(_random.NextDouble() * 800 + 150), 2),
                            AvailableSeats = availableSeats
                        });
                    }
                }
            }
        }
    }

    public Task<FlightSearchResponse> SearchFlightsAsync(FlightSearchRequest request)
    {
        // Ensure flights are up-to-date
        RegenerateFlightsIfNeeded();

        var matchingFlights = _flights
            .Where(f => f.Origin.Equals(request.Origin, StringComparison.OrdinalIgnoreCase) &&
                        f.Destination.Equals(request.Destination, StringComparison.OrdinalIgnoreCase) &&
                        DateOnly.FromDateTime(f.DepartureTime) == request.DepartureDate &&
                        f.AvailableSeats >= request.Passengers)
            .OrderBy(f => f.DepartureTime)
            .ToList();

        return Task.FromResult(new FlightSearchResponse
        {
            Flights = matchingFlights,
            TotalResults = matchingFlights.Count
        });
    }

    public Task<FlightListResponse> ListFlightsAsync(FlightListRequest request)
    {
        // Ensure flights are up-to-date
        RegenerateFlightsIfNeeded();

        var query = _flights.Where(f => DateOnly.FromDateTime(f.DepartureTime) == request.Date);

        // Apply optional filters
        if (!string.IsNullOrWhiteSpace(request.Origin))
        {
            query = query.Where(f => f.Origin.Equals(request.Origin, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            query = query.Where(f => f.Destination.Equals(request.Destination, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Airline))
        {
            query = query.Where(f => f.Airline.Contains(request.Airline, StringComparison.OrdinalIgnoreCase));
        }

        query = query.Where(f => f.AvailableSeats >= request.MinSeats);

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(f => f.Price <= request.MaxPrice.Value);
        }

        // Apply sorting
        query = request.SortBy.ToLowerInvariant() switch
        {
            "price" => query.OrderBy(f => f.Price),
            "duration" => query.OrderBy(f => f.ArrivalTime - f.DepartureTime),
            "airline" => query.OrderBy(f => f.Airline).ThenBy(f => f.DepartureTime),
            _ => query.OrderBy(f => f.DepartureTime) // default: departure
        };

        var totalResults = query.Count();
        var flights = query.Take(request.Limit).ToList();

        return Task.FromResult(new FlightListResponse
        {
            Flights = flights,
            TotalResults = totalResults,
            ReturnedResults = flights.Count,
            Date = request.Date,
            AppliedFilters = new FlightListFilters
            {
                Origin = request.Origin,
                Destination = request.Destination,
                Airline = request.Airline,
                MinSeats = request.MinSeats,
                MaxPrice = request.MaxPrice,
                SortBy = request.SortBy
            }
        });
    }

    public Task<Flight?> GetFlightByIdAsync(Guid flightId)
    {
        // Ensure flights are up-to-date
        RegenerateFlightsIfNeeded();
        
        return Task.FromResult(_flights.FirstOrDefault(f => f.Id == flightId));
    }

    public Task<FlightBooking> BookFlightAsync(FlightBookingRequest request)
    {
        RegenerateFlightsIfNeeded();
        
        var flight = _flights.FirstOrDefault(f => f.Id == request.FlightId)
            ?? throw new InvalidOperationException("Flight not found");

        if (flight.AvailableSeats < request.NumberOfSeats)
        {
            throw new InvalidOperationException("Not enough seats available");
        }

        var booking = new FlightBooking
        {
            FlightId = request.FlightId,
            PassengerName = request.PassengerName,
            PassengerEmail = request.PassengerEmail,
            NumberOfSeats = request.NumberOfSeats,
            TotalPrice = flight.Price * request.NumberOfSeats,
            ConfirmationCode = GenerateConfirmationCode()
        };

        _bookings[booking.BookingId] = booking;
        return Task.FromResult(booking);
    }

    public Task<FlightBooking?> GetBookingAsync(Guid bookingId)
    {
        return Task.FromResult(_bookings.GetValueOrDefault(bookingId));
    }

    private string GenerateConfirmationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }
}
