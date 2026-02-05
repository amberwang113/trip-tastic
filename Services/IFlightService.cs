using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IFlightService
{
    Task<FlightSearchResponse> SearchFlightsAsync(FlightSearchRequest request);
    Task<Flight?> GetFlightByIdAsync(Guid flightId);
    Task<FlightBooking> BookFlightAsync(FlightBookingRequest request);
    Task<FlightBooking?> GetBookingAsync(Guid bookingId);
}
