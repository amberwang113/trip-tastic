using trip_tastic.Models;

namespace trip_tastic.Services;

public interface ICartService
{
    void AddFlight(Flight flight, int passengers);
    void RemoveFlight(Guid flightId);
    void AddHotel(Hotel hotel, DateOnly checkInDate, DateOnly checkOutDate, int rooms, int guests);
    void RemoveHotel(Guid hotelId);
    void ClearCart();
    Cart GetCart();
    int GetItemCount();
    int GetReservedSeatsForFlight(Guid flightId);
    int GetReservedRoomsForHotel(Guid hotelId);
}
