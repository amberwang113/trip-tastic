using trip_tastic.Models;

namespace trip_tastic.Services;

public class CartService : ICartService
{
    private readonly Cart _cart = new();

    public void AddFlight(Flight flight, int passengers)
    {
        // Check if this flight is already in cart
        var existingItem = _cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Flight && i.Flight?.Id == flight.Id);
        
        if (existingItem is not null)
        {
            // Remove existing and add with updated passengers
            _cart.Items.Remove(existingItem);
        }

        _cart.Items.Add(new CartItem
        {
            Type = CartItemType.Flight,
            Flight = flight,
            Passengers = passengers
        });
    }

    public void RemoveFlight(Guid flightId)
    {
        var item = _cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Flight && i.Flight?.Id == flightId);
        
        if (item is not null)
        {
            _cart.Items.Remove(item);
        }
    }

    public void AddHotel(Hotel hotel, DateOnly checkInDate, DateOnly checkOutDate, int rooms, int guests)
    {
        // Check if this hotel with same dates is already in cart
        var existingItem = _cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Hotel && 
            i.Hotel?.Id == hotel.Id &&
            i.CheckInDate == checkInDate &&
            i.CheckOutDate == checkOutDate);
        
        if (existingItem is not null)
        {
            // Remove existing and add with updated details
            _cart.Items.Remove(existingItem);
        }

        _cart.Items.Add(new CartItem
        {
            Type = CartItemType.Hotel,
            Hotel = hotel,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Rooms = rooms,
            Guests = guests
        });
    }

    public void RemoveHotel(Guid hotelId)
    {
        var item = _cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Hotel && i.Hotel?.Id == hotelId);
        
        if (item is not null)
        {
            _cart.Items.Remove(item);
        }
    }

    public void ClearCart()
    {
        _cart.Items.Clear();
    }

    public Cart GetCart()
    {
        return _cart;
    }

    public int GetItemCount()
    {
        return _cart.TotalItems;
    }

    public int GetReservedSeatsForFlight(Guid flightId)
    {
        return _cart.Items
            .Where(i => i.Type == CartItemType.Flight && i.Flight?.Id == flightId)
            .Sum(i => i.Passengers);
    }

    public int GetReservedRoomsForHotel(Guid hotelId)
    {
        return _cart.Items
            .Where(i => i.Type == CartItemType.Hotel && i.Hotel?.Id == hotelId)
            .Sum(i => i.Rooms);
    }
}
