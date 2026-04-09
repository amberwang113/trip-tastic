using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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

// ---------------------------------------------------------------------------
// Authentication: Microsoft Entra ID (Azure AD)
//   - Browser (Razor Pages): OpenID Connect → Cookie authentication
//   - API callers: JWT Bearer token validation
//
// If AzureAd config is not provided (placeholder values), auth is disabled
// and the DevAuth handler is used so the site runs without Entra ID.
// ---------------------------------------------------------------------------
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var tenantId = azureAdSection["TenantId"];
var clientId = azureAdSection["ClientId"];
var authConfigured = !string.IsNullOrEmpty(tenantId)
                  && !string.IsNullOrEmpty(clientId)
                  && !tenantId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
                  && !clientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

if (!authConfigured)
{
    // No real auth configured — use DevAuth handler so site works without Entra ID
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = DevAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = DevAuthenticationHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
            DevAuthenticationHandler.SchemeName, _ => { });
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "DevOrBearer";
            options.DefaultChallengeScheme = "DevOrBearer";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var instance = azureAdSection["Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";
            options.Authority = $"{instance}/{tenantId}/v2.0";
            options.Audience = azureAdSection["Audience"] ?? clientId;
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
    // Production: OIDC implicit flow + Cookie for browser, JWT Bearer for API.
    // No client secret/cert/MSI needed — tokens are returned directly in the
    // browser redirect rather than exchanged via a back-channel code redemption.
    var instance = azureAdSection["Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "BrowserOrApi";
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = $"{instance}/{tenantId}/v2.0";
            options.ClientId = clientId;
            options.ResponseType = "id_token";       // Implicit flow — no code redemption
            options.UsePkce = false;                  // Not applicable for implicit flow
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.CallbackPath = azureAdSection["CallbackPath"] ?? "/signin-oidc";
            options.SignedOutCallbackPath = azureAdSection["SignedOutCallbackPath"] ?? "/signout-callback-oidc";

            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidIssuer = $"{instance}/{tenantId}/v2.0";

            // Map Entra ID 'roles' claim into ClaimTypes.Role for [Authorize(Roles = ...)]
            options.TokenValidationParameters.RoleClaimType = "roles";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = $"{instance}/{tenantId}/v2.0";
            options.Audience = azureAdSection["Audience"] ?? clientId;
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
                // Browser requests → Cookie (backed by OIDC implicit sign-in)
                return Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            };
        });

    // Only register MicrosoftIdentityUI routes when OpenIdConnect scheme is available
    builder.Services.AddRazorPages()
        .AddMicrosoftIdentityUI();
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
// Use DevUserContext when in Development or when auth is not configured
if (builder.Environment.IsDevelopment() || !authConfigured)
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
