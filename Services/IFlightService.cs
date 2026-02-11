using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IFlightService
{
    /// <summary>
    /// Get all available airports
    /// </summary>
    IEnumerable<AirportInfo> GetAvailableAirports();

    /// <summary>
    /// List flights for a specific day with optional filters
    /// </summary>
    Task<FlightListResponse> ListFlightsAsync(FlightListRequest request);

    /// <summary>
    /// Search for flights between specific airports
    /// </summary>
    Task<FlightSearchResponse> SearchFlightsAsync(FlightSearchRequest request);

    /// <summary>
    /// Get a specific flight by ID
    /// </summary>
    Task<Flight?> GetFlightByIdAsync(Guid flightId);

    /// <summary>
    /// Book a flight
    /// </summary>
    Task<FlightBooking> BookFlightAsync(FlightBookingRequest request);

    /// <summary>
    /// Get a booking by ID
    /// </summary>
    Task<FlightBooking?> GetBookingAsync(Guid bookingId);
}
