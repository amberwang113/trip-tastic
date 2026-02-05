using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IHotelService
{
    Task<HotelSearchResponse> SearchHotelsAsync(HotelSearchRequest request);
    Task<Hotel?> GetHotelByIdAsync(Guid hotelId);
    Task<HotelBooking> BookHotelAsync(HotelBookingRequest request);
    Task<HotelBooking?> GetBookingAsync(Guid bookingId);
}
