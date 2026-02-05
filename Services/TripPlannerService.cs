using trip_tastic.Models;

namespace trip_tastic.Services;

public class TripPlannerService : ITripPlannerService
{
    private readonly IFlightService _flightService;
    private readonly IHotelService _hotelService;

    // Map cities to their airport codes for the planner
    private static readonly Dictionary<string, string> CityToAirport = new(StringComparer.OrdinalIgnoreCase)
    {
        ["New York"] = "JFK",
        ["Los Angeles"] = "LAX",
        ["Chicago"] = "ORD",
        ["Dallas"] = "DFW",
        ["Denver"] = "DEN",
        ["San Francisco"] = "SFO",
        ["Seattle"] = "SEA",
        ["Miami"] = "MIA",
        ["Boston"] = "BOS",
        ["Atlanta"] = "ATL",
        ["London"] = "LHR",
        ["Paris"] = "CDG",
        ["Frankfurt"] = "FRA",
        ["Tokyo"] = "NRT",
        ["Sydney"] = "SYD"
    };

    private static readonly Dictionary<string, string> AirportToCity = 
        CityToAirport.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    public TripPlannerService(IFlightService flightService, IHotelService hotelService)
    {
        _flightService = flightService;
        _hotelService = hotelService;
    }

    public async Task<TripPlan> PlanTripAsync(TripPlanRequest request)
    {
        var nights = request.ReturnDate.DayNumber - request.DepartureDate.DayNumber;
        
        // Resolve destination to city name for hotel search
        var destinationCity = AirportToCity.GetValueOrDefault(request.Destination, request.Destination);
        
        // Execute all searches in parallel - this is what makes it powerful for MCP!
        var outboundFlightsTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
        {
            Origin = request.Origin,
            Destination = request.Destination,
            DepartureDate = request.DepartureDate,
            Passengers = request.Travelers
        });

        var returnFlightsTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
        {
            Origin = request.Destination,
            Destination = request.Origin,
            DepartureDate = request.ReturnDate,
            Passengers = request.Travelers
        });

        var hotelsTask = _hotelService.SearchHotelsAsync(new HotelSearchRequest
        {
            Location = destinationCity,
            CheckInDate = request.DepartureDate,
            CheckOutDate = request.ReturnDate,
            Guests = request.Travelers,
            Rooms = request.Rooms
        });

        // Wait for all searches to complete
        await Task.WhenAll(outboundFlightsTask, returnFlightsTask, hotelsTask);

        var outboundFlights = (await outboundFlightsTask).Flights;
        var returnFlights = (await returnFlightsTask).Flights;
        var hotels = (await hotelsTask).Hotels;

        // Generate recommendation based on best value
        TripRecommendation? recommendation = null;
        if (outboundFlights.Count > 0 && returnFlights.Count > 0 && hotels.Count > 0)
        {
            var bestOutbound = outboundFlights.OrderBy(f => f.Price).First();
            var bestReturn = returnFlights.OrderBy(f => f.Price).First();
            var bestHotel = hotels.OrderBy(h => h.TotalPrice).First();

            var totalPrice = (bestOutbound.Price + bestReturn.Price) * request.Travelers + bestHotel.TotalPrice;

            // Check budget constraint
            if (!request.MaxBudget.HasValue || totalPrice <= request.MaxBudget)
            {
                recommendation = new TripRecommendation
                {
                    OutboundFlight = bestOutbound,
                    ReturnFlight = bestReturn,
                    Hotel = bestHotel,
                    TotalPrice = totalPrice,
                    RecommendationReason = "Best value combination based on lowest total price"
                };
            }
            else
            {
                // Try to find a combination within budget
                foreach (var outbound in outboundFlights.OrderBy(f => f.Price))
                {
                    foreach (var ret in returnFlights.OrderBy(f => f.Price))
                    {
                        foreach (var hotel in hotels.OrderBy(h => h.TotalPrice))
                        {
                            var price = (outbound.Price + ret.Price) * request.Travelers + hotel.TotalPrice;
                            if (price <= request.MaxBudget)
                            {
                                recommendation = new TripRecommendation
                                {
                                    OutboundFlight = outbound,
                                    ReturnFlight = ret,
                                    Hotel = hotel,
                                    TotalPrice = price,
                                    RecommendationReason = $"Best option within your ${request.MaxBudget} budget"
                                };
                                break;
                            }
                        }
                        if (recommendation != null) break;
                    }
                    if (recommendation != null) break;
                }
            }
        }

        // Calculate summary statistics
        var cheapestOutbound = outboundFlights.MinBy(f => f.Price)?.Price ?? 0;
        var cheapestReturn = returnFlights.MinBy(f => f.Price)?.Price ?? 0;
        var cheapestHotel = hotels.MinBy(h => h.TotalPrice)?.TotalPrice ?? 0;

        return new TripPlan
        {
            Origin = request.Origin,
            Destination = request.Destination,
            DepartureDate = request.DepartureDate,
            ReturnDate = request.ReturnDate,
            Travelers = request.Travelers,
            Nights = nights,
            OutboundFlights = outboundFlights,
            ReturnFlights = returnFlights,
            Hotels = hotels,
            Recommendation = recommendation,
            Summary = new TripSummary
            {
                TotalOutboundFlights = outboundFlights.Count,
                TotalReturnFlights = returnFlights.Count,
                TotalHotels = hotels.Count,
                CheapestFlightPrice = (cheapestOutbound + cheapestReturn) * request.Travelers,
                CheapestHotelPrice = cheapestHotel,
                CheapestTotalTrip = (cheapestOutbound + cheapestReturn) * request.Travelers + cheapestHotel
            }
        };
    }

    public async Task<MultiCityTripPlan> PlanMultiCityTripAsync(MultiCityTripRequest request)
    {
        var legs = new List<CityLeg>();
        var totalCost = 0m;
        var totalNights = 0;

        // Build all legs including return to origin
        var allStops = new List<(string from, string to, DateOnly flightDate, DateOnly? hotelCheckIn, DateOnly? hotelCheckOut)>();
        
        var previousCity = request.Origin;
        foreach (var city in request.Cities)
        {
            var airportCode = CityToAirport.GetValueOrDefault(city.City, city.City);
            allStops.Add((previousCity, airportCode, city.ArrivalDate, city.ArrivalDate, city.DepartureDate));
            previousCity = airportCode;
        }
        
        // Add return leg
        var lastCity = request.Cities.Last();
        var lastAirport = CityToAirport.GetValueOrDefault(lastCity.City, lastCity.City);
        allStops.Add((lastAirport, request.Origin, lastCity.DepartureDate, null, null));

        // Process all legs in parallel
        var legTasks = allStops.Select(async stop =>
        {
            var flightTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = stop.from,
                Destination = stop.to,
                DepartureDate = stop.flightDate,
                Passengers = request.Travelers
            });

            Task<HotelSearchResponse>? hotelTask = null;
            if (stop.hotelCheckIn.HasValue && stop.hotelCheckOut.HasValue)
            {
                var hotelCity = AirportToCity.GetValueOrDefault(stop.to, stop.to);
                hotelTask = _hotelService.SearchHotelsAsync(new HotelSearchRequest
                {
                    Location = hotelCity,
                    CheckInDate = stop.hotelCheckIn.Value,
                    CheckOutDate = stop.hotelCheckOut.Value,
                    Guests = request.Travelers,
                    Rooms = 1
                });
            }

            var flights = await flightTask;
            var hotels = hotelTask != null ? await hotelTask : null;

            var recommendedFlight = flights.Flights.OrderBy(f => f.Price).FirstOrDefault();
            var recommendedHotel = hotels?.Hotels.OrderBy(h => h.TotalPrice).FirstOrDefault();

            return new CityLeg
            {
                From = stop.from,
                To = stop.to,
                Date = stop.flightDate,
                AvailableFlights = flights.Flights,
                AvailableHotels = hotels?.Hotels ?? [],
                RecommendedFlight = recommendedFlight,
                RecommendedHotel = recommendedHotel
            };
        });

        var completedLegs = await Task.WhenAll(legTasks);
        legs.AddRange(completedLegs);

        // Calculate totals
        foreach (var leg in legs)
        {
            if (leg.RecommendedFlight != null)
                totalCost += leg.RecommendedFlight.Price * request.Travelers;
            if (leg.RecommendedHotel != null)
            {
                totalCost += leg.RecommendedHotel.TotalPrice;
                totalNights += leg.RecommendedHotel.Nights;
            }
        }

        return new MultiCityTripPlan
        {
            Origin = request.Origin,
            Legs = legs,
            TotalEstimatedCost = totalCost,
            TotalNights = totalNights,
            TotalFlights = legs.Count
        };
    }

    public async Task<DealFinderResponse> FindDealsAsync(DealFinderRequest request)
    {
        var deals = new List<TripDeal>();
        var searchDate = request.StartDate;

        // Search multiple destinations and dates in parallel
        var searchTasks = new List<Task<TripDeal?>>();

        while (searchDate.AddDays(request.Nights) <= request.EndDate)
        {
            var departureDate = searchDate;
            var returnDate = searchDate.AddDays(request.Nights);

            foreach (var destination in request.Destinations)
            {
                var destAirport = CityToAirport.GetValueOrDefault(destination, destination);
                var destCity = AirportToCity.GetValueOrDefault(destination, destination);
                
                searchTasks.Add(SearchDealAsync(
                    request.Origin, 
                    destAirport, 
                    destCity,
                    departureDate, 
                    returnDate, 
                    request.Travelers,
                    request.Nights));
            }

            searchDate = searchDate.AddDays(1);
        }

        var results = await Task.WhenAll(searchTasks);
        deals.AddRange(results.Where(d => d != null)!);

        // Sort by price per person
        deals = deals.OrderBy(d => d.PricePerPerson).ToList();

        return new DealFinderResponse
        {
            Deals = deals,
            BestDeal = deals.FirstOrDefault()
        };
    }

    private async Task<TripDeal?> SearchDealAsync(
        string origin, 
        string destAirport,
        string destCity,
        DateOnly departureDate, 
        DateOnly returnDate, 
        int travelers,
        int nights)
    {
        try
        {
            var outboundTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = origin,
                Destination = destAirport,
                DepartureDate = departureDate,
                Passengers = travelers
            });

            var returnTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = destAirport,
                Destination = origin,
                DepartureDate = returnDate,
                Passengers = travelers
            });

            var hotelTask = _hotelService.SearchHotelsAsync(new HotelSearchRequest
            {
                Location = destCity,
                CheckInDate = departureDate,
                CheckOutDate = returnDate,
                Guests = travelers,
                Rooms = 1
            });

            await Task.WhenAll(outboundTask, returnTask, hotelTask);

            var outbound = (await outboundTask).Flights.OrderBy(f => f.Price).FirstOrDefault();
            var ret = (await returnTask).Flights.OrderBy(f => f.Price).FirstOrDefault();
            var hotel = (await hotelTask).Hotels.OrderBy(h => h.TotalPrice).FirstOrDefault();

            if (outbound == null || ret == null || hotel == null)
                return null;

            var totalPrice = (outbound.Price + ret.Price) * travelers + hotel.TotalPrice;

            return new TripDeal
            {
                Destination = destCity,
                DepartureDate = departureDate,
                ReturnDate = returnDate,
                OutboundFlight = outbound,
                ReturnFlight = ret,
                Hotel = hotel,
                TotalPrice = totalPrice,
                PricePerPerson = totalPrice / travelers,
                PricePerNight = totalPrice / nights
            };
        }
        catch
        {
            return null;
        }
    }
}
