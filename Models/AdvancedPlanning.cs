namespace trip_tastic.Models;

// ============================================
// Flexible Date Search - find cheapest dates
// ============================================

public record FlexibleDateSearchRequest
{
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public int Passengers { get; init; } = 1;
    public int TripLength { get; init; } = 3;
}

public record FlexibleDateSearchResponse
{
    public required IReadOnlyList<DatePriceOption> Options { get; init; }
    public DatePriceOption? CheapestOption { get; init; }
    public DatePriceOption? BestValueOption { get; init; }
    public required FlexibleDateSummary Summary { get; init; }
}

public record DatePriceOption
{
    public required DateOnly DepartureDate { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required Flight OutboundFlight { get; init; }
    public required Flight ReturnFlight { get; init; }
    public required decimal TotalFlightCost { get; init; }
    public required decimal PricePerPerson { get; init; }
    public required string DayOfWeek { get; init; }
    public bool IsWeekend { get; init; }
}

public record FlexibleDateSummary
{
    public decimal AveragePrice { get; init; }
    public decimal LowestPrice { get; init; }
    public decimal HighestPrice { get; init; }
    public decimal PotentialSavings { get; init; }
    public int TotalOptionsSearched { get; init; }
}

// ============================================
// Price Comparison - compare routes/airlines
// ============================================

public record PriceComparisonRequest
{
    public required string Origin { get; init; }
    public required IReadOnlyList<string> Destinations { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public int Travelers { get; init; } = 1;
    public bool IncludeHotels { get; init; } = true;
}

public record PriceComparisonResponse
{
    public required IReadOnlyList<DestinationComparison> Comparisons { get; init; }
    public DestinationComparison? CheapestDestination { get; init; }
    public DestinationComparison? BestValueDestination { get; init; }
    public required ComparisonSummary Summary { get; init; }
}

public record DestinationComparison
{
    public required string Destination { get; init; }
    public required string DestinationCity { get; init; }
    public required Flight CheapestOutboundFlight { get; init; }
    public required Flight CheapestReturnFlight { get; init; }
    public HotelAvailability? CheapestHotel { get; init; }
    public required decimal FlightCost { get; init; }
    public decimal? HotelCost { get; init; }
    public required decimal TotalCost { get; init; }
    public required int AvailableFlightOptions { get; init; }
    public int AvailableHotelOptions { get; init; }
}

public record ComparisonSummary
{
    public int DestinationsCompared { get; init; }
    public decimal CheapestTotalPrice { get; init; }
    public decimal MostExpensiveTotalPrice { get; init; }
    public decimal AveragePrice { get; init; }
}

// ============================================
// Budget Optimizer - maximize value in budget
// ============================================

public record BudgetOptimizerRequest
{
    public required string Origin { get; init; }
    public required IReadOnlyList<string> PreferredDestinations { get; init; }
    public required DateOnly EarliestDeparture { get; init; }
    public required DateOnly LatestReturn { get; init; }
    public required decimal Budget { get; init; }
    public int Travelers { get; init; } = 1;
    public int MinNights { get; init; } = 2;
    public int MaxNights { get; init; } = 7;
    public int MinHotelStars { get; init; } = 3;
}

public record BudgetOptimizerResponse
{
    public required IReadOnlyList<BudgetOption> Options { get; init; }
    public BudgetOption? BestOption { get; init; }
    public BudgetOption? LongestStayOption { get; init; }
    public BudgetOption? BestHotelOption { get; init; }
    public required BudgetSummary Summary { get; init; }
}

public record BudgetOption
{
    public required string Destination { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public required DateOnly ReturnDate { get; init; }
    public required int Nights { get; init; }
    public required Flight OutboundFlight { get; init; }
    public required Flight ReturnFlight { get; init; }
    public required HotelAvailability Hotel { get; init; }
    public required decimal TotalCost { get; init; }
    public required decimal RemainingBudget { get; init; }
    public required decimal ValueScore { get; init; }
    public required string ValueExplanation { get; init; }
}

public record BudgetSummary
{
    public decimal Budget { get; init; }
    public int TotalOptionsFound { get; init; }
    public int DestinationsWithinBudget { get; init; }
    public decimal AverageCostOfOptions { get; init; }
}

// ============================================
// Saved Itineraries - create and manage trips
// ============================================

public record CreateItineraryRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Origin { get; init; }
    public required IReadOnlyList<ItinerarySegment> Segments { get; init; }
    public int Travelers { get; init; } = 1;
}

public record ItinerarySegment
{
    public required string Destination { get; init; }
    public required DateOnly ArrivalDate { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public Guid? PreferredFlightId { get; init; }
    public Guid? PreferredHotelId { get; init; }
}

public record SavedItinerary
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Origin { get; init; }
    public required int Travelers { get; init; }
    public required IReadOnlyList<ItineraryLeg> Legs { get; init; }
    public required decimal EstimatedTotalCost { get; init; }
    public required int TotalNights { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastModified { get; init; }
    public ItineraryStatus Status { get; init; } = ItineraryStatus.Draft;
}

public record ItineraryLeg
{
    public int LegNumber { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public required DateOnly FlightDate { get; init; }
    public DateOnly? HotelCheckIn { get; init; }
    public DateOnly? HotelCheckOut { get; init; }
    public Flight? SelectedFlight { get; init; }
    public HotelAvailability? SelectedHotel { get; init; }
    public required IReadOnlyList<Flight> AlternativeFlights { get; init; }
    public required IReadOnlyList<HotelAvailability> AlternativeHotels { get; init; }
    public decimal LegCost { get; init; }
}

public enum ItineraryStatus
{
    Draft,
    Confirmed,
    Booked,
    Completed,
    Cancelled
}

public record UpdateItineraryRequest
{
    public Guid ItineraryId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Travelers { get; init; }
    public IReadOnlyList<ItinerarySegmentUpdate>? SegmentUpdates { get; init; }
}

public record ItinerarySegmentUpdate
{
    public int LegNumber { get; init; }
    public Guid? NewFlightId { get; init; }
    public Guid? NewHotelId { get; init; }
}

// ============================================
// Trip Analytics - insights about travel
// ============================================

public record TripAnalyticsRequest
{
    public required string Origin { get; init; }
    public required IReadOnlyList<string> Destinations { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}

public record TripAnalyticsResponse
{
    public required IReadOnlyList<DestinationAnalytics> DestinationInsights { get; init; }
    public required PriceTrendAnalysis PriceTrends { get; init; }
    public required IReadOnlyList<string> Recommendations { get; init; }
}

public record DestinationAnalytics
{
    public required string Destination { get; init; }
    public decimal AverageFlightPrice { get; init; }
    public decimal AverageHotelPricePerNight { get; init; }
    public string? CheapestDayToFly { get; init; }
    public int FlightOptionsCount { get; init; }
    public int HotelOptionsCount { get; init; }
}

public record PriceTrendAnalysis
{
    public decimal OverallAverageFlightPrice { get; init; }
    public decimal OverallAverageHotelPrice { get; init; }
    public string? BestDayOfWeekToBook { get; init; }
    public decimal WeekdayVsWeekendPriceDifference { get; init; }
}
