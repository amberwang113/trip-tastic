using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using trip_tastic.Middleware;
using trip_tastic.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI(); // Adds /MicrosoftIdentity/Account/SignIn & SignOut routes
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ---------------------------------------------------------------------------
// Authentication: Microsoft Entra ID (Azure AD)
//   - Browser (Razor Pages): OpenID Connect → Cookie authentication
//   - API callers: JWT Bearer token validation
//
// In Development, a DevAuth cookie-based scheme is also registered so the
// dev user switcher works with [Authorize] attributes without real JWTs.
// ---------------------------------------------------------------------------
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "DevOrBearer";
            options.DefaultChallengeScheme = "DevOrBearer";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var azureAdSection = builder.Configuration.GetSection("AzureAd");
            var tenantId = azureAdSection["TenantId"] ?? "common";
            var instance = azureAdSection["Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";
            options.Authority = $"{instance}/{tenantId}/v2.0";
            options.Audience = azureAdSection["Audience"] ?? azureAdSection["ClientId"];
            options.TokenValidationParameters.ValidateIssuer = true;
        })
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
            DevAuthenticationHandler.SchemeName, _ => { })
        .AddPolicyScheme("DevOrBearer", "Dev cookie or JWT Bearer", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                return DevAuthenticationHandler.SchemeName;
            };
        });
}
else
{
    // Production: OIDC + Cookie for browser, JWT Bearer for API
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "BrowserOrApi";
            options.DefaultChallengeScheme = "BrowserOrApi";
        })
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"),
            OpenIdConnectDefaults.AuthenticationScheme)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();

    builder.Services.AddAuthentication()
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var azureAdSection = builder.Configuration.GetSection("AzureAd");
            var tenantId = azureAdSection["TenantId"] ?? "common";
            var instance = azureAdSection["Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";
            options.Authority = $"{instance}/{tenantId}/v2.0";
            options.Audience = azureAdSection["Audience"] ?? azureAdSection["ClientId"];
            options.TokenValidationParameters.ValidateIssuer = true;
        })
        .AddPolicyScheme("BrowserOrApi", "Browser (OIDC cookie) or API (JWT Bearer)", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // API requests with a Bearer token → JWT Bearer scheme
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                // API paths without a browser session → JWT Bearer (returns 401 instead of redirect)
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                // Browser requests → OpenID Connect + Cookie
                return OpenIdConnectDefaults.AuthenticationScheme;
            };
        });
}

// ---------------------------------------------------------------------------
// Authorization: Role-Based Access Control (RBAC)
// Roles are sourced from the 'roles' claim in the JWT token.
// Assign App Roles in the Entra ID App Registration → App Roles blade:
//   - TripTastic.Admin  : Full access including debug/admin endpoints
//   - TripTastic.User   : Can search, book, manage own cart & trips
//   - TripTastic.Reader  : Read-only access to search & browse
// ---------------------------------------------------------------------------
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("TripTastic.Admin"))
    .AddPolicy("RequireUser", policy =>
        policy.RequireRole("TripTastic.User", "TripTastic.Admin"))
    .AddPolicy("RequireReader", policy =>
        policy.RequireRole("TripTastic.Reader", "TripTastic.User", "TripTastic.Admin"))
    .AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());

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

// Authentication & Authorization middleware (order matters)
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
