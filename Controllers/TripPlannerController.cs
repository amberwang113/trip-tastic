using Microsoft.AspNetCore.Mvc;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TripPlannerController : ControllerBase
{
    private readonly ITripPlannerService _tripPlannerService;

    public TripPlannerController(ITripPlannerService tripPlannerService)
    {
        _tripPlannerService = tripPlannerService;
    }

    /// <summary>
    /// Plan a complete round-trip with flights and hotel.
    /// This endpoint combines multiple searches (outbound flight, return flight, hotel) in a single call.
    /// Perfect for demonstrating AI tool composition.
    /// </summary>
    /// <param name="origin">Origin airport code (e.g., JFK)</param>
    /// <param name="destination">Destination airport code (e.g., LAX)</param>
    /// <param name="departureDate">Departure date (YYYY-MM-DD)</param>
    /// <param name="returnDate">Return date (YYYY-MM-DD)</param>
    /// <param name="travelers">Number of travelers (default: 1)</param>
    /// <param name="rooms">Number of hotel rooms (default: 1)</param>
    /// <param name="maxBudget">Optional maximum budget for the entire trip</param>
    /// <returns>Complete trip plan with flights, hotels, and recommendations</returns>
    [HttpGet("plan")]
    [ProducesResponseType(typeof(TripPlan), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripPlan>> PlanTrip(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] DateOnly departureDate,
        [FromQuery] DateOnly returnDate,
        [FromQuery] int travelers = 1,
        [FromQuery] int rooms = 1,
        [FromQuery] decimal? maxBudget = null)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest("Origin and destination are required");
        }

        if (returnDate <= departureDate)
        {
            return BadRequest("Return date must be after departure date");
        }

        var request = new TripPlanRequest
        {
            Origin = origin,
            Destination = destination,
            DepartureDate = departureDate,
            ReturnDate = returnDate,
            Travelers = travelers,
            Rooms = rooms,
            MaxBudget = maxBudget
        };

        var plan = await _tripPlannerService.PlanTripAsync(request);
        return Ok(plan);
    }

    /// <summary>
    /// Plan a multi-city trip visiting multiple destinations.
    /// Demonstrates complex itinerary planning with multiple parallel searches.
    /// </summary>
    /// <param name="request">Multi-city trip request with origin and city stops</param>
    /// <returns>Complete multi-city itinerary with flights and hotels for each leg</returns>
    [HttpPost("multi-city")]
    [ProducesResponseType(typeof(MultiCityTripPlan), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MultiCityTripPlan>> PlanMultiCityTrip([FromBody] MultiCityTripRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Origin))
        {
            return BadRequest("Origin is required");
        }

        if (request.Cities.Count == 0)
        {
            return BadRequest("At least one city stop is required");
        }

        var plan = await _tripPlannerService.PlanMultiCityTripAsync(request);
        return Ok(plan);
    }

    /// <summary>
    /// Find the best travel deals across multiple destinations and dates.
    /// Searches in parallel to find the cheapest options.
    /// </summary>
    /// <param name="origin">Origin airport code</param>
    /// <param name="destinations">Comma-separated list of destination cities</param>
    /// <param name="startDate">Start of date range to search</param>
    /// <param name="endDate">End of date range to search</param>
    /// <param name="travelers">Number of travelers (default: 1)</param>
    /// <param name="nights">Length of stay in nights (default: 3)</param>
    /// <returns>List of deals sorted by price, with best deal highlighted</returns>
    [HttpGet("deals")]
    [ProducesResponseType(typeof(DealFinderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DealFinderResponse>> FindDeals(
        [FromQuery] string origin,
        [FromQuery] string destinations,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int travelers = 1,
        [FromQuery] int nights = 3)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return BadRequest("Origin is required");
        }

        if (string.IsNullOrWhiteSpace(destinations))
        {
            return BadRequest("At least one destination is required");
        }

        if (endDate <= startDate)
        {
            return BadRequest("End date must be after start date");
        }

        var destinationList = destinations.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var request = new DealFinderRequest
        {
            Origin = origin,
            Destinations = destinationList,
            StartDate = startDate,
            EndDate = endDate,
            Travelers = travelers,
            Nights = nights
        };

        var deals = await _tripPlannerService.FindDealsAsync(request);
        return Ok(deals);
    }
}
