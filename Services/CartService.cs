using System.Collections.Concurrent;
using trip_tastic.Models;

namespace trip_tastic.Services;

public class CartService : ICartService
{
    private readonly ConcurrentDictionary<string, Cart> _userCarts = new();

    private Cart GetOrCreateCart(string userId)
    {
        return _userCarts.GetOrAdd(userId, _ => new Cart());
    }

    public void AddFlight(string userId, Flight flight, int passengers)
    {
        var cart = GetOrCreateCart(userId);
        
        // Check if this flight is already in cart
        var existingItem = cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Flight && i.Flight?.Id == flight.Id);
        
        if (existingItem is not null)
        {
            // Remove existing and add with updated passengers
            cart.Items.Remove(existingItem);
        }

        cart.Items.Add(new CartItem
        {
            Type = CartItemType.Flight,
            Flight = flight,
            Passengers = passengers
        });
    }

    public void RemoveFlight(string userId, Guid flightId)
    {
        var cart = GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Flight && i.Flight?.Id == flightId);
        
        if (item is not null)
        {
            cart.Items.Remove(item);
        }
    }

    public void AddHotel(string userId, Hotel hotel, DateOnly checkInDate, DateOnly checkOutDate, int rooms, int guests)
    {
        var cart = GetOrCreateCart(userId);
        
        // Check if this hotel with same dates is already in cart
        var existingItem = cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Hotel && 
            i.Hotel?.Id == hotel.Id &&
            i.CheckInDate == checkInDate &&
            i.CheckOutDate == checkOutDate);
        
        if (existingItem is not null)
        {
            // Remove existing and add with updated details
            cart.Items.Remove(existingItem);
        }

        cart.Items.Add(new CartItem
        {
            Type = CartItemType.Hotel,
            Hotel = hotel,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Rooms = rooms,
            Guests = guests
        });
    }

    public void RemoveHotel(string userId, Guid hotelId)
    {
        var cart = GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => 
            i.Type == CartItemType.Hotel && i.Hotel?.Id == hotelId);
        
        if (item is not null)
        {
            cart.Items.Remove(item);
        }
    }

    public void ClearCart(string userId)
    {
        var cart = GetOrCreateCart(userId);
        cart.Items.Clear();
    }

    public Cart GetCart(string userId)
    {
        return GetOrCreateCart(userId);
    }

    public int GetItemCount(string userId)
    {
        return GetOrCreateCart(userId).TotalItems;
    }

    public int GetReservedSeatsForFlight(Guid flightId)
    {
        return _userCarts.Values
            .SelectMany(c => c.Items)
            .Where(i => i.Type == CartItemType.Flight && i.Flight?.Id == flightId)
            .Sum(i => i.Passengers);
    }

    public int GetReservedRoomsForHotel(Guid hotelId)
    {
        return _userCarts.Values
            .SelectMany(c => c.Items)
            .Where(i => i.Type == CartItemType.Hotel && i.Hotel?.Id == hotelId)
            .Sum(i => i.Rooms);
    }
}
