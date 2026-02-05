namespace trip_tastic.Models;

public record TripPlanRequest
{
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public int Travelers { get; init; } = 1;
    public int Rooms { get; init; } = 1;
    public decimal? MaxBudget { get; init; }
}

public record TripPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required int Travelers { get; init; }
    public required int Nights { get; init; }
    
    public required IReadOnlyList<Flight> OutboundFlights { get; init; }
    public required IReadOnlyList<Flight> ReturnFlights { get; init; }
    public required IReadOnlyList<HotelAvailability> Hotels { get; init; }
    
    public TripRecommendation? Recommendation { get; init; }
    
    public TripSummary Summary { get; init; } = new();
}

public record TripRecommendation
{
    public required Flight OutboundFlight { get; init; }
    public required Flight ReturnFlight { get; init; }
    public required HotelAvailability Hotel { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string RecommendationReason { get; init; }
}

public record TripSummary
{
    public int TotalOutboundFlights { get; init; }
    public int TotalReturnFlights { get; init; }
    public int TotalHotels { get; init; }
    public decimal CheapestFlightPrice { get; init; }
    public decimal CheapestHotelPrice { get; init; }
    public decimal CheapestTotalTrip { get; init; }
}

public record MultiCityTripRequest
{
    public required string Origin { get; init; }
    public required IReadOnlyList<CityStop> Cities { get; init; }
    public int Travelers { get; init; } = 1;
}

public record CityStop
{
    public required string City { get; init; }
    public required DateOnly ArrivalDate { get; init; }
    public required DateOnly DepartureDate { get; init; }
}

public record MultiCityTripPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public required string Origin { get; init; }
    public required IReadOnlyList<CityLeg> Legs { get; init; }
    public required decimal TotalEstimatedCost { get; init; }
    public required int TotalNights { get; init; }
    public required int TotalFlights { get; init; }
}

public record CityLeg
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<Flight> AvailableFlights { get; init; }
    public required IReadOnlyList<HotelAvailability> AvailableHotels { get; init; }
    public Flight? RecommendedFlight { get; init; }
    public HotelAvailability? RecommendedHotel { get; init; }
}

public record DealFinderRequest
{
    public required string Origin { get; init; }
    public required IReadOnlyList<string> Destinations { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public int Travelers { get; init; } = 1;
    public int Nights { get; init; } = 3;
}

public record DealFinderResponse
{
    public required IReadOnlyList<TripDeal> Deals { get; init; }
    public TripDeal? BestDeal { get; init; }
}

public record TripDeal
{
    public required string Destination { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required Flight OutboundFlight { get; init; }
    public required Flight ReturnFlight { get; init; }
    public required HotelAvailability Hotel { get; init; }
    public required decimal TotalPrice { get; init; }
    public required decimal PricePerPerson { get; init; }
    public required decimal PricePerNight { get; init; }
}
