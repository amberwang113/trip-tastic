using Microsoft.AspNetCore.Mvc.RazorPages;
using trip_tastic.Models;

namespace trip_tastic.Pages.Destinations;

public class IndexModel : PageModel
{
    public IReadOnlyList<Destination> Destinations => DestinationData.All;

    public void OnGet()
    {
    }
}
