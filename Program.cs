using trip_tastic.Middleware;
using trip_tastic.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add CORS policy for API access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add HttpContextAccessor for user context
builder.Services.AddHttpContextAccessor();

// Register request logging service (singleton to persist across requests)
builder.Services.AddSingleton<RequestLogService>();

// Register user context service (scoped to handle per-request user identity)
// In Development, use DevUserContext to allow switching between simulated users
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<UserContext>();
    builder.Services.AddScoped<IUserContext, DevUserContext>();
}
else
{
    builder.Services.AddScoped<IUserContext, UserContext>();
}

// Register application services (singleton to maintain sample data)
builder.Services.AddSingleton<IFlightService, FlightService>();
builder.Services.AddSingleton<IHotelService, HotelService>();
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddSingleton<ITripPlannerService, TripPlannerService>();
builder.Services.AddSingleton<IAdvancedPlanningService, AdvancedPlanningService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add request logging middleware (captures all requests for debug page)
app.UseRequestLogging();

app.UseStaticFiles();

app.UseRouting();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
