using Microsoft.AspNetCore.Mvc;
using trip_tastic.Models;
using trip_tastic.Services;

namespace trip_tastic.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdvancedPlanningController : ControllerBase
{
    private readonly IAdvancedPlanningService _advancedPlanningService;

    public AdvancedPlanningController(IAdvancedPlanningService advancedPlanningService)
    {
        _advancedPlanningService = advancedPlanningService;
    }

    /// <summary>
    /// Search for flights across a flexible date range to find the cheapest travel dates.
    /// This endpoint runs parallel searches across multiple dates to identify price patterns.
    /// Perfect for AI agents helping users find the best time to travel.
    /// </summary>
    /// <param name="origin">Origin airport code (e.g., JFK)</param>
    /// <param name="destination">Destination airport code (e.g., LAX)</param>
    /// <param name="startDate">Start of date range to search (YYYY-MM-DD)</param>
    /// <param name="endDate">End of date range to search (YYYY-MM-DD)</param>
    /// <param name="passengers">Number of passengers (default: 1)</param>
    /// <param name="tripLength">Length of trip in days (default: 3)</param>
    /// <returns>List of date options with prices, sorted by cost</returns>
    [HttpGet("flexible-dates")]
    [ProducesResponseType(typeof(FlexibleDateSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FlexibleDateSearchResponse>> SearchFlexibleDates(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int passengers = 1,
        [FromQuery] int tripLength = 3)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest("Origin and destination are required");
        }

        if (endDate <= startDate)
        {
            return BadRequest("End date must be after start date");
        }

        var request = new FlexibleDateSearchRequest
        {
            Origin = origin,
            Destination = destination,
            StartDate = startDate,
            EndDate = endDate,
            Passengers = passengers,
            TripLength = tripLength
        };

        var result = await _advancedPlanningService.SearchFlexibleDatesAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Compare prices across multiple destinations for the same travel dates.
    /// Helps users decide where to go based on their budget.
    /// Demonstrates parallel destination comparison with optional hotel inclusion.
    /// </summary>
    /// <param name="origin">Origin airport code</param>
    /// <param name="destinations">Comma-separated list of destination cities or airport codes</param>
    /// <param name="departureDate">Departure date (YYYY-MM-DD)</param>
    /// <param name="returnDate">Return date (YYYY-MM-DD)</param>
    /// <param name="travelers">Number of travelers (default: 1)</param>
    /// <param name="includeHotels">Whether to include hotel prices (default: true)</param>
    /// <returns>Price comparison across all destinations</returns>
    [HttpGet("compare-destinations")]
    [ProducesResponseType(typeof(PriceComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceComparisonResponse>> CompareDestinations(
        [FromQuery] string origin,
        [FromQuery] string destinations,
        [FromQuery] DateOnly departureDate,
        [FromQuery] DateOnly returnDate,
        [FromQuery] int travelers = 1,
        [FromQuery] bool includeHotels = true)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return BadRequest("Origin is required");
        }

        if (string.IsNullOrWhiteSpace(destinations))
        {
            return BadRequest("At least one destination is required");
        }

        if (returnDate <= departureDate)
        {
            return BadRequest("Return date must be after departure date");
        }

        var destinationList = destinations.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var request = new PriceComparisonRequest
        {
            Origin = origin,
            Destinations = destinationList,
            DepartureDate = departureDate,
            ReturnDate = returnDate,
            Travelers = travelers,
            IncludeHotels = includeHotels
        };

        var result = await _advancedPlanningService.CompareDestinationsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Find the best trip options within a specified budget.
    /// Searches across multiple destinations and dates to maximize value.
    /// Returns options ranked by a value score considering hotel quality, trip length, and remaining budget.
    /// </summary>
    /// <param name="origin">Origin airport code</param>
    /// <param name="destinations">Comma-separated list of preferred destination cities</param>
    /// <param name="earliestDeparture">Earliest possible departure date (YYYY-MM-DD)</param>
    /// <param name="latestReturn">Latest possible return date (YYYY-MM-DD)</param>
    /// <param name="budget">Maximum budget in USD</param>
    /// <param name="travelers">Number of travelers (default: 1)</param>
    /// <param name="minNights">Minimum nights for the trip (default: 2)</param>
    /// <param name="maxNights">Maximum nights for the trip (default: 7)</param>
    /// <param name="minHotelStars">Minimum hotel star rating (default: 3)</param>
    /// <returns>Budget-optimized trip options with value scores</returns>
    [HttpGet("optimize-budget")]
    [ProducesResponseType(typeof(BudgetOptimizerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetOptimizerResponse>> OptimizeBudget(
        [FromQuery] string origin,
        [FromQuery] string destinations,
        [FromQuery] DateOnly earliestDeparture,
        [FromQuery] DateOnly latestReturn,
        [FromQuery] decimal budget,
        [FromQuery] int travelers = 1,
        [FromQuery] int minNights = 2,
        [FromQuery] int maxNights = 7,
        [FromQuery] int minHotelStars = 3)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return BadRequest("Origin is required");
        }

        if (string.IsNullOrWhiteSpace(destinations))
        {
            return BadRequest("At least one destination is required");
        }

        if (budget <= 0)
        {
            return BadRequest("Budget must be greater than zero");
        }

        if (latestReturn <= earliestDeparture)
        {
            return BadRequest("Latest return must be after earliest departure");
        }

        var destinationList = destinations.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var request = new BudgetOptimizerRequest
        {
            Origin = origin,
            PreferredDestinations = destinationList,
            EarliestDeparture = earliestDeparture,
            LatestReturn = latestReturn,
            Budget = budget,
            Travelers = travelers,
            MinNights = minNights,
            MaxNights = maxNights,
            MinHotelStars = minHotelStars
        };

        var result = await _advancedPlanningService.OptimizeBudgetAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Create a new saved itinerary with multiple destination segments.
    /// The itinerary automatically finds flights and hotels for each segment.
    /// Perfect for AI agents building complex multi-city trips.
    /// </summary>
    /// <param name="request">Itinerary creation request with segments</param>
    /// <returns>The created itinerary with all flight and hotel options</returns>
    [HttpPost("itineraries")]
    [ProducesResponseType(typeof(SavedItinerary), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavedItinerary>> CreateItinerary([FromBody] CreateItineraryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Itinerary name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Origin))
        {
            return BadRequest("Origin is required");
        }

        if (request.Segments.Count == 0)
        {
            return BadRequest("At least one segment is required");
        }

        var itinerary = await _advancedPlanningService.CreateItineraryAsync(request);
        return CreatedAtAction(nameof(GetItinerary), new { id = itinerary.Id }, itinerary);
    }

    /// <summary>
    /// Get a saved itinerary by ID.
    /// </summary>
    /// <param name="id">Itinerary ID</param>
    /// <returns>The saved itinerary</returns>
    [HttpGet("itineraries/{id}")]
    [ProducesResponseType(typeof(SavedItinerary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedItinerary>> GetItinerary(Guid id)
    {
        var itinerary = await _advancedPlanningService.GetItineraryAsync(id);
        if (itinerary == null)
        {
            return NotFound();
        }
        return Ok(itinerary);
    }

    /// <summary>
    /// Get all saved itineraries.
    /// Returns itineraries sorted by creation date (newest first).
    /// </summary>
    /// <returns>List of all saved itineraries</returns>
    [HttpGet("itineraries")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedItinerary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SavedItinerary>>> GetAllItineraries()
    {
        var itineraries = await _advancedPlanningService.GetAllItinerariesAsync();
        return Ok(itineraries);
    }

    /// <summary>
    /// Update an existing itinerary.
    /// Can update name, description, travelers, or swap out flights/hotels for specific legs.
    /// </summary>
    /// <param name="id">Itinerary ID</param>
    /// <param name="request">Update request with changes</param>
    /// <returns>The updated itinerary</returns>
    [HttpPut("itineraries/{id}")]
    [ProducesResponseType(typeof(SavedItinerary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavedItinerary>> UpdateItinerary(Guid id, [FromBody] UpdateItineraryRequest request)
    {
        if (request.ItineraryId != id)
        {
            request = request with { ItineraryId = id };
        }

        var itinerary = await _advancedPlanningService.UpdateItineraryAsync(request);
        if (itinerary == null)
        {
            return NotFound();
        }
        return Ok(itinerary);
    }

    /// <summary>
    /// Delete a saved itinerary.
    /// </summary>
    /// <param name="id">Itinerary ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("itineraries/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItinerary(Guid id)
    {
        var deleted = await _advancedPlanningService.DeleteItineraryAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Get analytics and insights about travel options.
    /// Analyzes prices across destinations and dates to provide recommendations.
    /// Useful for AI agents providing travel advice.
    /// </summary>
    /// <param name="origin">Origin airport code</param>
    /// <param name="destinations">Comma-separated list of destinations to analyze</param>
    /// <param name="startDate">Start of analysis period (YYYY-MM-DD)</param>
    /// <param name="endDate">End of analysis period (YYYY-MM-DD)</param>
    /// <returns>Analytics with insights and recommendations</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(TripAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripAnalyticsResponse>> GetAnalytics(
        [FromQuery] string origin,
        [FromQuery] string destinations,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
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

        var request = new TripAnalyticsRequest
        {
            Origin = origin,
            Destinations = destinationList,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _advancedPlanningService.GetTripAnalyticsAsync(request);
        return Ok(result);
    }
}
