namespace trip_tastic.Models;

public record Destination
{
    public required string AirportCode { get; init; }
    public required string CityName { get; init; }
    public required string Country { get; init; }
    public required string Description { get; init; }
    public required string Tagline { get; init; }
    public required string ImageUrl { get; init; }
    public required string[] Highlights { get; init; }
    public required string Climate { get; init; }
    public required string BestTimeToVisit { get; init; }
    public required string Language { get; init; }
    public required string Currency { get; init; }
    public required string TimeZone { get; init; }
}

public static class DestinationData
{
    public static IReadOnlyList<Destination> All { get; } =
    [
        new Destination
        {
            AirportCode = "JFK",
            CityName = "New York",
            Country = "United States",
            Description = "New York City, the city that never sleeps, is a global hub of culture, finance, and entertainment. From the iconic Statue of Liberty to the bright lights of Times Square, NYC offers an unparalleled urban experience with world-class museums, Broadway shows, and diverse culinary scene.",
            Tagline = "The city that never sleeps",
            ImageUrl = "https://picsum.photos/seed/nyc/800/400",
            Highlights = ["Statue of Liberty", "Central Park", "Times Square", "Empire State Building", "Broadway", "Metropolitan Museum of Art"],
            Climate = "Humid subtropical with four distinct seasons",
            BestTimeToVisit = "April to June, September to November",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Eastern Time (ET)"
        },
        new Destination
        {
            AirportCode = "LAX",
            CityName = "Los Angeles",
            Country = "United States",
            Description = "Los Angeles, the entertainment capital of the world, offers sunny beaches, Hollywood glamour, and endless sunshine. From the Santa Monica Pier to the Hollywood Hills, LA is a sprawling metropolis with diverse neighborhoods, world-famous studios, and a vibrant arts scene.",
            Tagline = "The entertainment capital of the world",
            ImageUrl = "https://picsum.photos/seed/losangeles/800/400",
            Highlights = ["Hollywood Sign", "Santa Monica Beach", "Universal Studios", "Getty Center", "Venice Beach", "Griffith Observatory"],
            Climate = "Mediterranean climate with warm, dry summers",
            BestTimeToVisit = "March to May, September to November",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Pacific Time (PT)"
        },
        new Destination
        {
            AirportCode = "ORD",
            CityName = "Chicago",
            Country = "United States",
            Description = "Chicago, the Windy City, is known for its stunning architecture, deep-dish pizza, and vibrant music scene. Nestled along Lake Michigan, this metropolis boasts world-class museums, beautiful parks, and a rich cultural heritage that blends Midwestern charm with big-city sophistication.",
            Tagline = "The Windy City",
            ImageUrl = "https://picsum.photos/seed/chicago/800/400",
            Highlights = ["Millennium Park", "Willis Tower", "Art Institute of Chicago", "Navy Pier", "Magnificent Mile", "Wrigley Field"],
            Climate = "Humid continental with cold winters and warm summers",
            BestTimeToVisit = "April to May, September to October",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Central Time (CT)"
        },
        new Destination
        {
            AirportCode = "DFW",
            CityName = "Dallas",
            Country = "United States",
            Description = "Dallas, a modern metropolis in North Texas, combines Southern hospitality with big-city amenities. Known for its thriving arts district, diverse culinary scene, and rich history, Dallas offers world-class shopping, professional sports, and iconic Texas culture.",
            Tagline = "Big things happen here",
            ImageUrl = "https://picsum.photos/seed/dallas/800/400",
            Highlights = ["Dallas Arts District", "Sixth Floor Museum", "AT&T Stadium", "Dallas Arboretum", "Reunion Tower", "Deep Ellum"],
            Climate = "Humid subtropical with hot summers",
            BestTimeToVisit = "March to May, October to November",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Central Time (CT)"
        },
        new Destination
        {
            AirportCode = "DEN",
            CityName = "Denver",
            Country = "United States",
            Description = "Denver, the Mile High City, sits at the base of the majestic Rocky Mountains. This outdoor enthusiast's paradise offers world-class skiing, hiking, and a thriving craft beer scene. With over 300 days of sunshine, Denver combines urban culture with easy access to nature.",
            Tagline = "The Mile High City",
            ImageUrl = "https://picsum.photos/seed/denver/800/400",
            Highlights = ["Rocky Mountain National Park", "Red Rocks Amphitheatre", "Denver Art Museum", "16th Street Mall", "Larimer Square", "Denver Botanic Gardens"],
            Climate = "Semi-arid with mild winters and warm summers",
            BestTimeToVisit = "June to August, December to March for skiing",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Mountain Time (MT)"
        },
        new Destination
        {
            AirportCode = "SFO",
            CityName = "San Francisco",
            Country = "United States",
            Description = "San Francisco, the City by the Bay, captivates visitors with its iconic Golden Gate Bridge, Victorian architecture, and tech innovation. From riding historic cable cars to exploring Fisherman's Wharf, SF offers a unique blend of natural beauty and cosmopolitan culture.",
            Tagline = "The City by the Bay",
            ImageUrl = "https://picsum.photos/seed/sanfrancisco/800/400",
            Highlights = ["Golden Gate Bridge", "Alcatraz Island", "Fisherman's Wharf", "Chinatown", "Cable Cars", "Golden Gate Park"],
            Climate = "Mediterranean with cool, foggy summers",
            BestTimeToVisit = "September to November",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Pacific Time (PT)"
        },
        new Destination
        {
            AirportCode = "SEA",
            CityName = "Seattle",
            Country = "United States",
            Description = "Seattle, the Emerald City, is nestled between Puget Sound and the Cascade Mountains. Known for its coffee culture, tech industry, and stunning natural scenery, Seattle offers a perfect blend of urban sophistication and outdoor adventure in the beautiful Pacific Northwest.",
            Tagline = "The Emerald City",
            ImageUrl = "https://picsum.photos/seed/seattle/800/400",
            Highlights = ["Space Needle", "Pike Place Market", "Museum of Pop Culture", "Seattle Waterfront", "Capitol Hill", "Olympic National Park"],
            Climate = "Oceanic with mild, rainy winters",
            BestTimeToVisit = "June to September",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Pacific Time (PT)"
        },
        new Destination
        {
            AirportCode = "MIA",
            CityName = "Miami",
            Country = "United States",
            Description = "Miami, the Magic City, is a vibrant tropical metropolis where Latin culture meets Art Deco glamour. With its stunning beaches, world-famous nightlife, and diverse neighborhoods, Miami offers year-round sunshine and an energetic atmosphere unlike anywhere else in America.",
            Tagline = "The Magic City",
            ImageUrl = "https://picsum.photos/seed/miami/800/400",
            Highlights = ["South Beach", "Art Deco Historic District", "Wynwood Walls", "Little Havana", "Vizcaya Museum", "Everglades National Park"],
            Climate = "Tropical monsoon with warm, humid weather year-round",
            BestTimeToVisit = "December to May",
            Language = "English, Spanish",
            Currency = "USD ($)",
            TimeZone = "Eastern Time (ET)"
        },
        new Destination
        {
            AirportCode = "BOS",
            CityName = "Boston",
            Country = "United States",
            Description = "Boston, the Cradle of Liberty, is one of America's oldest and most historic cities. Home to world-renowned universities, the Freedom Trail, and passionate sports fans, Boston combines colonial charm with modern innovation in a walkable, culturally rich setting.",
            Tagline = "The Cradle of Liberty",
            ImageUrl = "https://picsum.photos/seed/boston/800/400",
            Highlights = ["Freedom Trail", "Fenway Park", "Harvard University", "Boston Common", "Faneuil Hall", "New England Aquarium"],
            Climate = "Humid continental with cold winters and warm summers",
            BestTimeToVisit = "June to October",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Eastern Time (ET)"
        },
        new Destination
        {
            AirportCode = "ATL",
            CityName = "Atlanta",
            Country = "United States",
            Description = "Atlanta, the capital of the South, is a dynamic city with deep historical roots and modern ambition. From the birthplace of Martin Luther King Jr. to its thriving music and film industry, Atlanta offers Southern hospitality, diverse cuisine, and endless entertainment.",
            Tagline = "The capital of the South",
            ImageUrl = "https://picsum.photos/seed/atlanta/800/400",
            Highlights = ["Georgia Aquarium", "World of Coca-Cola", "Martin Luther King Jr. Historic Site", "Piedmont Park", "Atlanta BeltLine", "High Museum of Art"],
            Climate = "Humid subtropical with hot summers",
            BestTimeToVisit = "March to May, September to November",
            Language = "English",
            Currency = "USD ($)",
            TimeZone = "Eastern Time (ET)"
        },
        new Destination
        {
            AirportCode = "LHR",
            CityName = "London",
            Country = "United Kingdom",
            Description = "London, a global city steeped in history, seamlessly blends centuries-old traditions with cutting-edge modernity. From the Tower of London to the West End theatres, London offers world-class museums, royal palaces, diverse cuisine, and iconic landmarks at every turn.",
            Tagline = "Where history meets the future",
            ImageUrl = "https://picsum.photos/seed/london/800/400",
            Highlights = ["Big Ben", "Tower of London", "British Museum", "Buckingham Palace", "London Eye", "Westminster Abbey"],
            Climate = "Temperate oceanic with mild temperatures year-round",
            BestTimeToVisit = "May to September",
            Language = "English",
            Currency = "GBP (£)",
            TimeZone = "Greenwich Mean Time (GMT)"
        },
        new Destination
        {
            AirportCode = "CDG",
            CityName = "Paris",
            Country = "France",
            Description = "Paris, the City of Light, is the epitome of romance, art, and elegance. From the Eiffel Tower to the Louvre, Paris enchants visitors with its stunning architecture, world-famous cuisine, haute couture fashion, and an artistic heritage that has inspired generations.",
            Tagline = "The City of Light",
            ImageUrl = "https://picsum.photos/seed/paris/800/400",
            Highlights = ["Eiffel Tower", "Louvre Museum", "Notre-Dame", "Champs-Élysées", "Montmartre", "Palace of Versailles"],
            Climate = "Oceanic with mild winters and warm summers",
            BestTimeToVisit = "April to June, September to October",
            Language = "French",
            Currency = "EUR (€)",
            TimeZone = "Central European Time (CET)"
        },
        new Destination
        {
            AirportCode = "FRA",
            CityName = "Frankfurt",
            Country = "Germany",
            Description = "Frankfurt, Germany's financial capital, is a dynamic city where medieval history meets modern skyscrapers. Home to the European Central Bank and world-class museums, Frankfurt offers a unique blend of old-world charm and contemporary sophistication in the heart of Europe.",
            Tagline = "The financial heart of Europe",
            ImageUrl = "https://picsum.photos/seed/frankfurt/800/400",
            Highlights = ["Römer", "Main Tower", "Städel Museum", "Palmengarten", "Old Town", "Frankfurt Cathedral"],
            Climate = "Oceanic with mild winters and warm summers",
            BestTimeToVisit = "May to September",
            Language = "German",
            Currency = "EUR (€)",
            TimeZone = "Central European Time (CET)"
        },
        new Destination
        {
            AirportCode = "NRT",
            CityName = "Tokyo",
            Country = "Japan",
            Description = "Tokyo, Japan's dazzling capital, is where ancient traditions meet ultramodern innovation. From serene temples and cherry blossoms to neon-lit streets and cutting-edge technology, Tokyo offers an unforgettable journey through a culture that honors its past while embracing the future.",
            Tagline = "Where tradition meets the future",
            ImageUrl = "https://picsum.photos/seed/tokyo/800/400",
            Highlights = ["Senso-ji Temple", "Shibuya Crossing", "Tokyo Skytree", "Meiji Shrine", "Akihabara", "Tsukiji Outer Market"],
            Climate = "Humid subtropical with hot summers and mild winters",
            BestTimeToVisit = "March to May, September to November",
            Language = "Japanese",
            Currency = "JPY (¥)",
            TimeZone = "Japan Standard Time (JST)"
        },
        new Destination
        {
            AirportCode = "SYD",
            CityName = "Sydney",
            Country = "Australia",
            Description = "Sydney, Australia's harbor city, is renowned for its stunning natural beauty and iconic landmarks. From the world-famous Opera House to sun-kissed Bondi Beach, Sydney offers a perfect blend of outdoor adventures, cultural experiences, and laid-back Australian hospitality.",
            Tagline = "The harbor city wonder",
            ImageUrl = "https://picsum.photos/seed/sydney/800/400",
            Highlights = ["Sydney Opera House", "Sydney Harbour Bridge", "Bondi Beach", "Taronga Zoo", "The Rocks", "Royal Botanic Garden"],
            Climate = "Humid subtropical with warm summers and mild winters",
            BestTimeToVisit = "September to November, March to May",
            Language = "English",
            Currency = "AUD ($)",
            TimeZone = "Australian Eastern Standard Time (AEST)"
        }
    ];

    public static IReadOnlyList<string> AirportCodes => All.Select(d => d.AirportCode).ToList();
    
    public static IReadOnlyList<string> CityNames => All.Select(d => d.CityName).ToList();

    public static Destination? GetByAirportCode(string airportCode) =>
        All.FirstOrDefault(d => d.AirportCode.Equals(airportCode, StringComparison.OrdinalIgnoreCase));

    public static Destination? GetByCityName(string cityName) =>
        All.FirstOrDefault(d => d.CityName.Equals(cityName, StringComparison.OrdinalIgnoreCase));

    public static string? GetAirportCodeForCity(string cityName) =>
        GetByCityName(cityName)?.AirportCode;

    public static string? GetCityNameForAirport(string airportCode) =>
        GetByAirportCode(airportCode)?.CityName;
}
