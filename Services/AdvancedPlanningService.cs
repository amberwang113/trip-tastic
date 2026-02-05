using System.Collections.Concurrent;
using trip_tastic.Models;

namespace trip_tastic.Services;

public class AdvancedPlanningService : IAdvancedPlanningService
{
    private readonly IFlightService _flightService;
    private readonly IHotelService _hotelService;
    private readonly ConcurrentDictionary<Guid, SavedItinerary> _savedItineraries = new();

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

    public AdvancedPlanningService(IFlightService flightService, IHotelService hotelService)
    {
        _flightService = flightService;
        _hotelService = hotelService;
    }

    public async Task<FlexibleDateSearchResponse> SearchFlexibleDatesAsync(FlexibleDateSearchRequest request)
    {
        var options = new List<DatePriceOption>();
        var searchTasks = new List<Task<DatePriceOption?>>();

        var currentDate = request.StartDate;
        while (currentDate.AddDays(request.TripLength) <= request.EndDate)
        {
            var departureDate = currentDate;
            var returnDate = currentDate.AddDays(request.TripLength);

            searchTasks.Add(SearchDateOptionAsync(
                request.Origin,
                request.Destination,
                departureDate,
                returnDate,
                request.Passengers));

            currentDate = currentDate.AddDays(1);
        }

        var results = await Task.WhenAll(searchTasks);
        options.AddRange(results.Where(r => r != null)!);

        options = options.OrderBy(o => o.TotalFlightCost).ToList();

        var cheapest = options.FirstOrDefault();
        var bestValue = options
            .Where(o => !o.IsWeekend)
            .OrderBy(o => o.TotalFlightCost)
            .FirstOrDefault() ?? cheapest;

        var prices = options.Select(o => o.TotalFlightCost).ToList();

        return new FlexibleDateSearchResponse
        {
            Options = options,
            CheapestOption = cheapest,
            BestValueOption = bestValue,
            Summary = new FlexibleDateSummary
            {
                AveragePrice = prices.Count > 0 ? prices.Average() : 0,
                LowestPrice = prices.Count > 0 ? prices.Min() : 0,
                HighestPrice = prices.Count > 0 ? prices.Max() : 0,
                PotentialSavings = prices.Count > 0 ? prices.Max() - prices.Min() : 0,
                TotalOptionsSearched = options.Count
            }
        };
    }

    private async Task<DatePriceOption?> SearchDateOptionAsync(
        string origin, string destination, DateOnly departureDate, DateOnly returnDate, int passengers)
    {
        try
        {
            var outboundTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = origin,
                Destination = destination,
                DepartureDate = departureDate,
                Passengers = passengers
            });

            var returnTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = destination,
                Destination = origin,
                DepartureDate = returnDate,
                Passengers = passengers
            });

            await Task.WhenAll(outboundTask, returnTask);

            var outbound = (await outboundTask).Flights.OrderBy(f => f.Price).FirstOrDefault();
            var returnFlight = (await returnTask).Flights.OrderBy(f => f.Price).FirstOrDefault();

            if (outbound == null || returnFlight == null)
                return null;

            var totalCost = (outbound.Price + returnFlight.Price) * passengers;
            var dayOfWeek = departureDate.DayOfWeek;
            var isWeekend = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            return new DatePriceOption
            {
                DepartureDate = departureDate,
                ReturnDate = returnDate,
                OutboundFlight = outbound,
                ReturnFlight = returnFlight,
                TotalFlightCost = totalCost,
                PricePerPerson = totalCost / passengers,
                DayOfWeek = dayOfWeek.ToString(),
                IsWeekend = isWeekend
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<PriceComparisonResponse> CompareDestinationsAsync(PriceComparisonRequest request)
    {
        var comparisons = new List<DestinationComparison>();

        var comparisonTasks = request.Destinations.Select(async dest =>
        {
            var airport = CityToAirport.GetValueOrDefault(dest, dest);
            var city = AirportToCity.GetValueOrDefault(dest, dest);

            var outboundTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = request.Origin,
                Destination = airport,
                DepartureDate = request.DepartureDate,
                Passengers = request.Travelers
            });

            var returnTask = _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = airport,
                Destination = request.Origin,
                DepartureDate = request.ReturnDate,
                Passengers = request.Travelers
            });

            Task<HotelSearchResponse>? hotelTask = null;
            if (request.IncludeHotels)
            {
                hotelTask = _hotelService.SearchHotelsAsync(new HotelSearchRequest
                {
                    Location = city,
                    CheckInDate = request.DepartureDate,
                    CheckOutDate = request.ReturnDate,
                    Guests = request.Travelers,
                    Rooms = 1
                });
            }

            await Task.WhenAll(
                outboundTask,
                returnTask,
                hotelTask ?? Task.CompletedTask);

            var outboundFlights = await outboundTask;
            var returnFlights = await returnTask;
            var hotels = hotelTask != null ? await hotelTask : null;

            var cheapestOutbound = outboundFlights.Flights.OrderBy(f => f.Price).FirstOrDefault();
            var cheapestReturn = returnFlights.Flights.OrderBy(f => f.Price).FirstOrDefault();
            var cheapestHotel = hotels?.Hotels.OrderBy(h => h.TotalPrice).FirstOrDefault();

            if (cheapestOutbound == null || cheapestReturn == null)
                return null;

            var flightCost = (cheapestOutbound.Price + cheapestReturn.Price) * request.Travelers;
            var hotelCost = cheapestHotel?.TotalPrice;
            var totalCost = flightCost + (hotelCost ?? 0);

            return new DestinationComparison
            {
                Destination = airport,
                DestinationCity = city,
                CheapestOutboundFlight = cheapestOutbound,
                CheapestReturnFlight = cheapestReturn,
                CheapestHotel = cheapestHotel,
                FlightCost = flightCost,
                HotelCost = hotelCost,
                TotalCost = totalCost,
                AvailableFlightOptions = outboundFlights.Flights.Count + returnFlights.Flights.Count,
                AvailableHotelOptions = hotels?.Hotels.Count ?? 0
            };
        });

        var results = await Task.WhenAll(comparisonTasks);
        comparisons.AddRange(results.Where(r => r != null)!);
        comparisons = comparisons.OrderBy(c => c.TotalCost).ToList();

        var cheapest = comparisons.FirstOrDefault();
        var bestValue = comparisons
            .Where(c => c.AvailableFlightOptions >= 2 && c.AvailableHotelOptions >= 2)
            .OrderBy(c => c.TotalCost)
            .FirstOrDefault() ?? cheapest;

        return new PriceComparisonResponse
        {
            Comparisons = comparisons,
            CheapestDestination = cheapest,
            BestValueDestination = bestValue,
            Summary = new ComparisonSummary
            {
                DestinationsCompared = comparisons.Count,
                CheapestTotalPrice = comparisons.Count > 0 ? comparisons.Min(c => c.TotalCost) : 0,
                MostExpensiveTotalPrice = comparisons.Count > 0 ? comparisons.Max(c => c.TotalCost) : 0,
                AveragePrice = comparisons.Count > 0 ? comparisons.Average(c => c.TotalCost) : 0
            }
        };
    }

    public async Task<BudgetOptimizerResponse> OptimizeBudgetAsync(BudgetOptimizerRequest request)
    {
        var options = new List<BudgetOption>();
        var searchTasks = new List<Task<BudgetOption?>>();

        foreach (var dest in request.PreferredDestinations)
        {
            var airport = CityToAirport.GetValueOrDefault(dest, dest);
            var city = AirportToCity.GetValueOrDefault(dest, dest);

            var currentDate = request.EarliestDeparture;
            while (currentDate.AddDays(request.MinNights) <= request.LatestReturn)
            {
                for (var nights = request.MinNights; nights <= request.MaxNights; nights++)
                {
                    var returnDate = currentDate.AddDays(nights);
                    if (returnDate > request.LatestReturn)
                        break;

                    searchTasks.Add(SearchBudgetOptionAsync(
                        request.Origin,
                        airport,
                        city,
                        currentDate,
                        returnDate,
                        nights,
                        request.Travelers,
                        request.Budget,
                        request.MinHotelStars));
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        var results = await Task.WhenAll(searchTasks);
        options.AddRange(results.Where(r => r != null)!);

        options = options.OrderByDescending(o => o.ValueScore).ToList();

        var bestOption = options.FirstOrDefault();
        var longestStay = options.OrderByDescending(o => o.Nights).FirstOrDefault();
        var bestHotel = options
            .Where(o => o.Hotel.Hotel.StarRating >= 4)
            .OrderByDescending(o => o.Hotel.Hotel.StarRating)
            .ThenBy(o => o.TotalCost)
            .FirstOrDefault();

        return new BudgetOptimizerResponse
        {
            Options = options.Take(20).ToList(),
            BestOption = bestOption,
            LongestStayOption = longestStay,
            BestHotelOption = bestHotel,
            Summary = new BudgetSummary
            {
                Budget = request.Budget,
                TotalOptionsFound = options.Count,
                DestinationsWithinBudget = options.Select(o => o.Destination).Distinct().Count(),
                AverageCostOfOptions = options.Count > 0 ? options.Average(o => o.TotalCost) : 0
            }
        };
    }

    private async Task<BudgetOption?> SearchBudgetOptionAsync(
        string origin, string destAirport, string destCity,
        DateOnly departureDate, DateOnly returnDate, int nights,
        int travelers, decimal budget, int minStars)
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
            var returnFlight = (await returnTask).Flights.OrderBy(f => f.Price).FirstOrDefault();
            var hotels = (await hotelTask).Hotels
                .Where(h => h.Hotel.StarRating >= minStars)
                .OrderBy(h => h.TotalPrice)
                .ToList();

            if (outbound == null || returnFlight == null || hotels.Count == 0)
                return null;

            var hotel = hotels.First();
            var flightCost = (outbound.Price + returnFlight.Price) * travelers;
            var totalCost = flightCost + hotel.TotalPrice;

            if (totalCost > budget)
                return null;

            var remainingBudget = budget - totalCost;
            var valueScore = CalculateValueScore(hotel.Hotel.StarRating, nights, remainingBudget, budget);

            return new BudgetOption
            {
                Destination = destCity,
                DepartureDate = departureDate,
                ReturnDate = returnDate,
                Nights = nights,
                OutboundFlight = outbound,
                ReturnFlight = returnFlight,
                Hotel = hotel,
                TotalCost = totalCost,
                RemainingBudget = remainingBudget,
                ValueScore = valueScore,
                ValueExplanation = GenerateValueExplanation(hotel.Hotel.StarRating, nights, remainingBudget, valueScore)
            };
        }
        catch
        {
            return null;
        }
    }

    private static decimal CalculateValueScore(int starRating, int nights, decimal remainingBudget, decimal totalBudget)
    {
        var starScore = starRating * 15m;
        var nightScore = nights * 10m;
        var budgetEfficiency = (remainingBudget / totalBudget) * 25m;
        return starScore + nightScore + budgetEfficiency;
    }

    private static string GenerateValueExplanation(int starRating, int nights, decimal remainingBudget, decimal valueScore)
    {
        var explanations = new List<string>();

        if (starRating >= 4)
            explanations.Add($"{starRating}-star luxury accommodation");
        else if (starRating == 3)
            explanations.Add("Comfortable 3-star hotel");

        if (nights >= 5)
            explanations.Add($"Extended {nights}-night stay");
        else
            explanations.Add($"{nights}-night getaway");

        if (remainingBudget > 200)
            explanations.Add($"${remainingBudget:F0} left for activities");

        return string.Join(", ", explanations) + $". Value score: {valueScore:F1}";
    }

    public async Task<SavedItinerary> CreateItineraryAsync(CreateItineraryRequest request)
    {
        var legs = new List<ItineraryLeg>();
        var totalCost = 0m;
        var totalNights = 0;
        var legNumber = 1;

        var previousLocation = request.Origin;

        foreach (var segment in request.Segments)
        {
            var airport = CityToAirport.GetValueOrDefault(segment.Destination, segment.Destination);
            var city = AirportToCity.GetValueOrDefault(segment.Destination, segment.Destination);

            var flightResponse = await _flightService.SearchFlightsAsync(new FlightSearchRequest
            {
                Origin = previousLocation,
                Destination = airport,
                DepartureDate = segment.ArrivalDate,
                Passengers = request.Travelers
            });

            var hotelResponse = await _hotelService.SearchHotelsAsync(new HotelSearchRequest
            {
                Location = city,
                CheckInDate = segment.ArrivalDate,
                CheckOutDate = segment.DepartureDate,
                Guests = request.Travelers,
                Rooms = 1
            });

            var selectedFlight = segment.PreferredFlightId.HasValue
                ? flightResponse.Flights.FirstOrDefault(f => f.Id == segment.PreferredFlightId)
                    ?? flightResponse.Flights.OrderBy(f => f.Price).FirstOrDefault()
                : flightResponse.Flights.OrderBy(f => f.Price).FirstOrDefault();

            var selectedHotel = segment.PreferredHotelId.HasValue
                ? hotelResponse.Hotels.FirstOrDefault(h => h.Hotel.Id == segment.PreferredHotelId)
                    ?? hotelResponse.Hotels.OrderBy(h => h.TotalPrice).FirstOrDefault()
                : hotelResponse.Hotels.OrderBy(h => h.TotalPrice).FirstOrDefault();

            var legCost = (selectedFlight?.Price ?? 0) * request.Travelers + (selectedHotel?.TotalPrice ?? 0);
            var nights = segment.DepartureDate.DayNumber - segment.ArrivalDate.DayNumber;

            legs.Add(new ItineraryLeg
            {
                LegNumber = legNumber++,
                From = previousLocation,
                To = airport,
                FlightDate = segment.ArrivalDate,
                HotelCheckIn = segment.ArrivalDate,
                HotelCheckOut = segment.DepartureDate,
                SelectedFlight = selectedFlight,
                SelectedHotel = selectedHotel,
                AlternativeFlights = flightResponse.Flights.Where(f => f.Id != selectedFlight?.Id).ToList(),
                AlternativeHotels = hotelResponse.Hotels.Where(h => h.Hotel.Id != selectedHotel?.Hotel.Id).ToList(),
                LegCost = legCost
            });

            totalCost += legCost;
            totalNights += nights;
            previousLocation = airport;
        }

        // Add return leg
        var returnFlights = await _flightService.SearchFlightsAsync(new FlightSearchRequest
        {
            Origin = previousLocation,
            Destination = request.Origin,
            DepartureDate = request.Segments.Last().DepartureDate,
            Passengers = request.Travelers
        });

        var returnFlight = returnFlights.Flights.OrderBy(f => f.Price).FirstOrDefault();
        var returnLegCost = (returnFlight?.Price ?? 0) * request.Travelers;

        legs.Add(new ItineraryLeg
        {
            LegNumber = legNumber,
            From = previousLocation,
            To = request.Origin,
            FlightDate = request.Segments.Last().DepartureDate,
            HotelCheckIn = null,
            HotelCheckOut = null,
            SelectedFlight = returnFlight,
            SelectedHotel = null,
            AlternativeFlights = returnFlights.Flights.Where(f => f.Id != returnFlight?.Id).ToList(),
            AlternativeHotels = [],
            LegCost = returnLegCost
        });

        totalCost += returnLegCost;

        var itinerary = new SavedItinerary
        {
            Name = request.Name,
            Description = request.Description,
            Origin = request.Origin,
            Travelers = request.Travelers,
            Legs = legs,
            EstimatedTotalCost = totalCost,
            TotalNights = totalNights,
            Status = ItineraryStatus.Draft
        };

        _savedItineraries[itinerary.Id] = itinerary;
        return itinerary;
    }

    public Task<SavedItinerary?> GetItineraryAsync(Guid itineraryId)
    {
        _savedItineraries.TryGetValue(itineraryId, out var itinerary);
        return Task.FromResult(itinerary);
    }

    public Task<IReadOnlyList<SavedItinerary>> GetAllItinerariesAsync()
    {
        return Task.FromResult<IReadOnlyList<SavedItinerary>>(
            _savedItineraries.Values.OrderByDescending(i => i.CreatedAt).ToList());
    }

    public async Task<SavedItinerary?> UpdateItineraryAsync(UpdateItineraryRequest request)
    {
        if (!_savedItineraries.TryGetValue(request.ItineraryId, out var existing))
            return null;

        var updatedLegs = existing.Legs.ToList();
        var newTotalCost = existing.EstimatedTotalCost;

        if (request.SegmentUpdates != null)
        {
            foreach (var update in request.SegmentUpdates)
            {
                var legIndex = updatedLegs.FindIndex(l => l.LegNumber == update.LegNumber);
                if (legIndex < 0) continue;

                var leg = updatedLegs[legIndex];
                var oldCost = leg.LegCost;
                Flight? newFlight = leg.SelectedFlight;
                HotelAvailability? newHotel = leg.SelectedHotel;

                if (update.NewFlightId.HasValue)
                {
                    newFlight = leg.AlternativeFlights.FirstOrDefault(f => f.Id == update.NewFlightId)
                        ?? await _flightService.GetFlightByIdAsync(update.NewFlightId.Value);
                }

                if (update.NewHotelId.HasValue && leg.HotelCheckIn.HasValue && leg.HotelCheckOut.HasValue)
                {
                    var hotelResponse = await _hotelService.SearchHotelsAsync(new HotelSearchRequest
                    {
                        Location = AirportToCity.GetValueOrDefault(leg.To, leg.To),
                        CheckInDate = leg.HotelCheckIn.Value,
                        CheckOutDate = leg.HotelCheckOut.Value,
                        Guests = existing.Travelers,
                        Rooms = 1
                    });
                    newHotel = hotelResponse.Hotels.FirstOrDefault(h => h.Hotel.Id == update.NewHotelId);
                }

                var newLegCost = (newFlight?.Price ?? 0) * existing.Travelers + (newHotel?.TotalPrice ?? 0);

                updatedLegs[legIndex] = leg with
                {
                    SelectedFlight = newFlight,
                    SelectedHotel = newHotel,
                    LegCost = newLegCost
                };

                newTotalCost = newTotalCost - oldCost + newLegCost;
            }
        }

        var updatedItinerary = existing with
        {
            Name = request.Name ?? existing.Name,
            Description = request.Description ?? existing.Description,
            Travelers = request.Travelers ?? existing.Travelers,
            Legs = updatedLegs,
            EstimatedTotalCost = newTotalCost,
            LastModified = DateTime.UtcNow
        };

        _savedItineraries[request.ItineraryId] = updatedItinerary;
        return updatedItinerary;
    }

    public Task<bool> DeleteItineraryAsync(Guid itineraryId)
    {
        return Task.FromResult(_savedItineraries.TryRemove(itineraryId, out _));
    }

    public async Task<TripAnalyticsResponse> GetTripAnalyticsAsync(TripAnalyticsRequest request)
    {
        var destinationInsights = new List<DestinationAnalytics>();
        var allFlightPrices = new List<decimal>();
        var allHotelPrices = new List<decimal>();
        var weekdayPrices = new List<decimal>();
        var weekendPrices = new List<decimal>();

        foreach (var dest in request.Destinations)
        {
            var airport = CityToAirport.GetValueOrDefault(dest, dest);
            var city = AirportToCity.GetValueOrDefault(dest, dest);

            var flightPrices = new List<decimal>();
            var hotelPrices = new List<decimal>();
            var dayPrices = new Dictionary<DayOfWeek, List<decimal>>();

            var currentDate = request.StartDate;
            while (currentDate <= request.EndDate)
            {
                var flights = await _flightService.SearchFlightsAsync(new FlightSearchRequest
                {
                    Origin = request.Origin,
                    Destination = airport,
                    DepartureDate = currentDate,
                    Passengers = 1
                });

                foreach (var flight in flights.Flights)
                {
                    flightPrices.Add(flight.Price);
                    allFlightPrices.Add(flight.Price);

                    if (!dayPrices.ContainsKey(currentDate.DayOfWeek))
                        dayPrices[currentDate.DayOfWeek] = new List<decimal>();
                    dayPrices[currentDate.DayOfWeek].Add(flight.Price);

                    if (currentDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        weekendPrices.Add(flight.Price);
                    else
                        weekdayPrices.Add(flight.Price);
                }

                if (currentDate.AddDays(1) <= request.EndDate)
                {
                    var hotels = await _hotelService.SearchHotelsAsync(new HotelSearchRequest
                    {
                        Location = city,
                        CheckInDate = currentDate,
                        CheckOutDate = currentDate.AddDays(1),
                        Guests = 1,
                        Rooms = 1
                    });

                    foreach (var hotel in hotels.Hotels)
                    {
                        hotelPrices.Add(hotel.Hotel.PricePerNight);
                        allHotelPrices.Add(hotel.Hotel.PricePerNight);
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            var cheapestDay = dayPrices
                .OrderBy(kvp => kvp.Value.Average())
                .FirstOrDefault();

            destinationInsights.Add(new DestinationAnalytics
            {
                Destination = city,
                AverageFlightPrice = flightPrices.Count > 0 ? flightPrices.Average() : 0,
                AverageHotelPricePerNight = hotelPrices.Count > 0 ? hotelPrices.Average() : 0,
                CheapestDayToFly = cheapestDay.Key.ToString(),
                FlightOptionsCount = flightPrices.Count,
                HotelOptionsCount = hotelPrices.Count
            });
        }

        var recommendations = GenerateRecommendations(destinationInsights, weekdayPrices, weekendPrices);

        return new TripAnalyticsResponse
        {
            DestinationInsights = destinationInsights,
            PriceTrends = new PriceTrendAnalysis
            {
                OverallAverageFlightPrice = allFlightPrices.Count > 0 ? allFlightPrices.Average() : 0,
                OverallAverageHotelPrice = allHotelPrices.Count > 0 ? allHotelPrices.Average() : 0,
                BestDayOfWeekToBook = "Tuesday",
                WeekdayVsWeekendPriceDifference = weekdayPrices.Count > 0 && weekendPrices.Count > 0
                    ? weekendPrices.Average() - weekdayPrices.Average()
                    : 0
            },
            Recommendations = recommendations
        };
    }

    private static List<string> GenerateRecommendations(
        List<DestinationAnalytics> insights,
        List<decimal> weekdayPrices,
        List<decimal> weekendPrices)
    {
        var recommendations = new List<string>();

        var cheapestDest = insights.OrderBy(i => i.AverageFlightPrice + i.AverageHotelPricePerNight).FirstOrDefault();
        if (cheapestDest != null)
        {
            recommendations.Add($"Best value destination: {cheapestDest.Destination} with avg flight ${cheapestDest.AverageFlightPrice:F0} and hotel ${cheapestDest.AverageHotelPricePerNight:F0}/night");
        }

        if (weekdayPrices.Count > 0 && weekendPrices.Count > 0)
        {
            var savings = weekendPrices.Average() - weekdayPrices.Average();
            if (savings > 20)
            {
                recommendations.Add($"Save an average of ${savings:F0} by flying on weekdays instead of weekends");
            }
        }

        recommendations.Add("Book Tuesday or Wednesday for typically lower prices");
        recommendations.Add("Consider flexible dates to find the best deals");

        return recommendations;
    }
}
