using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IBookingService
{
    /// <summary>
    /// Checkout the current cart and create a booked trip for the specified user.
    /// </summary>
    BookedTrip Checkout(Cart cart, string userId, string? userName = null);

    /// <summary>
    /// Get all booked trips for a specific user.
    /// </summary>
    IReadOnlyList<BookedTrip> GetTripsForUser(string userId);

    /// <summary>
    /// Get a specific booked trip by ID, only if it belongs to the specified user.
    /// </summary>
    BookedTrip? GetTrip(Guid tripId, string userId);

    /// <summary>
    /// Cancel a booked trip, only if it belongs to the specified user.
    /// </summary>
    bool CancelTrip(Guid tripId, string userId);

    /// <summary>
    /// Get the count of booked trips for a specific user.
    /// </summary>
    int GetTripCount(string userId);
}
