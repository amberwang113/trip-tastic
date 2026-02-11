using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IHotelService
{
    /// <summary>
    /// Get all available locations with hotels
    /// </summary>
    IEnumerable<LocationInfo> GetAvailableLocations();

    /// <summary>
    /// List hotels with optional filters (no date range required)
    /// </summary>
    Task<HotelListResponse> ListHotelsAsync(HotelListRequest request);

    /// <summary>
    /// Search for hotels with pricing for a specific stay
    /// </summary>
    Task<HotelSearchResponse> SearchHotelsAsync(HotelSearchRequest request);

    /// <summary>
    /// Get a specific hotel by ID
    /// </summary>
    Task<Hotel?> GetHotelByIdAsync(Guid hotelId);

    /// <summary>
    /// Book a hotel
    /// </summary>
    Task<HotelBooking> BookHotelAsync(HotelBookingRequest request);

    /// <summary>
    /// Get a booking by ID
    /// </summary>
    Task<HotelBooking?> GetBookingAsync(Guid bookingId);
}
