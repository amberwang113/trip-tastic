namespace trip_tastic.Models;

public class Cart
{
    public List<CartItem> Items { get; set; } = [];
    
    public decimal TotalPrice => Items.Sum(i => i.TotalPrice);
    
    public int TotalItems => Items.Count;
}

public class CartItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required CartItemType Type { get; init; }
    public Flight? Flight { get; init; }
    public int Passengers { get; init; }
    public Hotel? Hotel { get; init; }
    public DateOnly? CheckInDate { get; init; }
    public DateOnly? CheckOutDate { get; init; }
    public int Rooms { get; init; }
    public int Guests { get; init; }
    public DateTime AddedAt { get; init; } = DateTime.UtcNow;
    
    public int Nights => CheckInDate.HasValue && CheckOutDate.HasValue 
        ? CheckOutDate.Value.DayNumber - CheckInDate.Value.DayNumber 
        : 0;
    
    public decimal TotalPrice => Type switch
    {
        CartItemType.Flight => Flight?.Price * Passengers ?? 0,
        CartItemType.Hotel => Hotel?.PricePerNight * Nights * Rooms ?? 0,
        _ => 0
    };
    
    public string Description => Type switch
    {
        CartItemType.Flight => $"{Flight?.Origin} -> {Flight?.Destination} ({Flight?.FlightNumber})",
        CartItemType.Hotel => $"{Hotel?.Name} - {Nights} night(s)",
        _ => "Unknown item"
    };
}

public enum CartItemType
{
    Flight,
    Hotel
}
