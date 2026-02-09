using trip_tastic.Models;

namespace trip_tastic.Services;

public interface ICartService
{
    void AddFlight(string userId, Flight flight, int passengers);
    void RemoveFlight(string userId, Guid flightId);
    void AddHotel(string userId, Hotel hotel, DateOnly checkInDate, DateOnly checkOutDate, int rooms, int guests);
    void RemoveHotel(string userId, Guid hotelId);
    void ClearCart(string userId);
    Cart GetCart(string userId);
    int GetItemCount(string userId);
    int GetReservedSeatsForFlight(Guid flightId);
    int GetReservedRoomsForHotel(Guid hotelId);
}
