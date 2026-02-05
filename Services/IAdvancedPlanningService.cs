using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IAdvancedPlanningService
{
    /// <summary>
    /// Search for flights across a flexible date range to find the cheapest travel dates.
    /// Demonstrates parallel date searching and price comparison.
    /// </summary>
    Task<FlexibleDateSearchResponse> SearchFlexibleDatesAsync(FlexibleDateSearchRequest request);

    /// <summary>
    /// Compare prices across multiple destinations for the same dates.
    /// Helps users decide where to go based on budget.
    /// </summary>
    Task<PriceComparisonResponse> CompareDestinationsAsync(PriceComparisonRequest request);

    /// <summary>
    /// Given a budget, find the best possible trip options.
    /// Optimizes for value within constraints.
    /// </summary>
    Task<BudgetOptimizerResponse> OptimizeBudgetAsync(BudgetOptimizerRequest request);

    /// <summary>
    /// Create a new saved itinerary with multiple segments.
    /// </summary>
    Task<SavedItinerary> CreateItineraryAsync(CreateItineraryRequest request);

    /// <summary>
    /// Get a saved itinerary by ID.
    /// </summary>
    Task<SavedItinerary?> GetItineraryAsync(Guid itineraryId);

    /// <summary>
    /// Get all saved itineraries.
    /// </summary>
    Task<IReadOnlyList<SavedItinerary>> GetAllItinerariesAsync();

    /// <summary>
    /// Update an existing itinerary.
    /// </summary>
    Task<SavedItinerary?> UpdateItineraryAsync(UpdateItineraryRequest request);

    /// <summary>
    /// Delete a saved itinerary.
    /// </summary>
    Task<bool> DeleteItineraryAsync(Guid itineraryId);

    /// <summary>
    /// Get analytics and insights about travel options.
    /// </summary>
    Task<TripAnalyticsResponse> GetTripAnalyticsAsync(TripAnalyticsRequest request);
}
