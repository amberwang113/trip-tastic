using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;

namespace trip_tastic.Pages.Destinations;

public class DetailsModel : PageModel
{
    public Destination? Destination { get; private set; }
    public IEnumerable<Destination> OtherDestinations { get; private set; } = [];
    
    public string Tomorrow => DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd");
    public string DayAfterTomorrow => DateOnly.FromDateTime(DateTime.Today.AddDays(2)).ToString("yyyy-MM-dd");

    public IActionResult OnGet(string code)
    {
        Destination = DestinationData.GetByAirportCode(code);
        
        if (Destination is null)
        {
            return NotFound();
        }

        // Get random other destinations for recommendations
        OtherDestinations = DestinationData.All
            .Where(d => d.AirportCode != code)
            .OrderBy(_ => Random.Shared.Next())
            .Take(4);

        return Page();
    }
}
