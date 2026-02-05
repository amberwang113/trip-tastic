using trip_tastic.Models;

namespace trip_tastic.Services;

public interface ITripPlannerService
{
    /// <summary>
    /// Plans a complete round-trip with flights and hotel in a single call.
    /// Demonstrates combining multiple search operations.
    /// </summary>
    Task<TripPlan> PlanTripAsync(TripPlanRequest request);
    
    /// <summary>
    /// Plans a multi-city trip visiting multiple destinations.
    /// Demonstrates complex multi-step planning.
    /// </summary>
    Task<MultiCityTripPlan> PlanMultiCityTripAsync(MultiCityTripRequest request);
    
    /// <summary>
    /// Finds the best deals across multiple destinations and dates.
    /// Demonstrates parallel search and comparison.
    /// </summary>
    Task<DealFinderResponse> FindDealsAsync(DealFinderRequest request);
}
