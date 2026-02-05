using trip_tastic.Models;

namespace trip_tastic.Services;

public interface IBookingService
{
    /// <summary>
    /// Checkout the current cart and create a booked trip.
    /// </summary>
    BookedTrip Checkout(Cart cart);

    /// <summary>
    /// Get all booked trips.
    /// </summary>
    IReadOnlyList<BookedTrip> GetAllTrips();

    /// <summary>
    /// Get a specific booked trip by ID.
    /// </summary>
    BookedTrip? GetTrip(Guid tripId);

    /// <summary>
    /// Cancel a booked trip.
    /// </summary>
    bool CancelTrip(Guid tripId);

    /// <summary>
    /// Get the count of booked trips.
    /// </summary>
    int GetTripCount();
}
