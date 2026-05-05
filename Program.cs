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
var enableAuth = builder.Configuration.GetValue<bool>("EnableAuth", false);
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var tenantId = azureAdSection["TenantId"];
var clientId = azureAdSection["ClientId"];
var authConfigured = enableAuth
                  && !string.IsNullOrEmpty(tenantId)
                  && !string.IsNullOrEmpty(clientId)
                  && !tenantId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
                  && !clientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

if (!authConfigured)
{
    // No real auth configured — use DevAuth + EasyAuth (App Service built-in auth).
    // If X-MS-CLIENT-PRINCIPAL header is present (App Service EasyAuth), use that;
    // otherwise fall back to DevAuth so the site works locally without Entra ID.
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "DevOrEasyAuth";
            options.DefaultChallengeScheme = "DevOrEasyAuth";
        })
        .AddScheme<AuthenticationSchemeOptions, EasyAuthHandler>(
            EasyAuthHandler.SchemeName, _ => { })
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
            DevAuthenticationHandler.SchemeName, _ => { })
        .AddPolicyScheme("DevOrEasyAuth", "EasyAuth (App Service) or Dev cookie", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // App Service EasyAuth injects X-MS-CLIENT-PRINCIPAL header
                if (context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL"))
                {
                    return EasyAuthHandler.SchemeName;
                }
                return DevAuthenticationHandler.SchemeName;
            };
        });
}
else
{
    // Auth configured: OIDC implicit flow + Cookie for browser, JWT Bearer for API,
    // with EasyAuth as a fallback when running on App Service.
    var instance = azureAdSection["Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "BrowserOrApi";
            options.DefaultChallengeScheme = "BrowserOrApi";
            options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(cookieOptions =>
        {
            // When Cookie auth needs to challenge (unauthenticated browser user),
            // forward to OIDC which redirects to Entra ID login page.
            cookieOptions.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme;
        })
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
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidAudiences = new[]
            {
                clientId,
                $"api://{clientId}",
            };
        })
        .AddScheme<AuthenticationSchemeOptions, EasyAuthHandler>(
            EasyAuthHandler.SchemeName, _ => { })
        .AddPolicyScheme("BrowserOrApi", "Browser (OIDC cookie), API (JWT Bearer), or EasyAuth fallback", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // 1. Bearer token → JWT Bearer (app's own auth)
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                // 2. Existing OIDC cookie → Cookie scheme (app's own browser auth)
                if (context.Request.Cookies.ContainsKey(".AspNetCore.Cookies"))
                {
                    return Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                }
                // 3. EasyAuth headers present → fall back to App Service identity
                if (context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL"))
                {
                    return EasyAuthHandler.SchemeName;
                }
                // 4. API paths without any auth → JWT Bearer (returns 401 instead of redirect)
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                // 5. Browser requests → Cookie (will trigger OIDC challenge)
                return Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            };
        });

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
var authzBuilder = builder.Services.AddAuthorizationBuilder();
if (authConfigured)
{
    // All policies just require authentication — no role checks
    authzBuilder
        .AddPolicy("RequireAdmin", policy =>
            policy.RequireAuthenticatedUser())
        .AddPolicy("RequireUser", policy =>
            policy.RequireAuthenticatedUser())
        .AddPolicy("RequireReader", policy =>
            policy.RequireAuthenticatedUser())
        .AddPolicy("RequireAuthenticated", policy =>
            policy.RequireAuthenticatedUser());
}
else
{
    // Auth disabled — all policies pass through (allow everyone)
    authzBuilder
        .AddPolicy("RequireAdmin", policy =>
            policy.RequireAssertion(_ => true))
        .AddPolicy("RequireUser", policy =>
            policy.RequireAssertion(_ => true))
        .AddPolicy("RequireReader", policy =>
            policy.RequireAssertion(_ => true))
        .AddPolicy("RequireAuthenticated", policy =>
            policy.RequireAssertion(_ => true));
}

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

// Register WWW-Authenticate challenge state (toggleable at runtime)
var enableWwwAuth = builder.Configuration.GetValue<bool>("EnableWwwAuthenticate", false);
builder.Services.AddSingleton(new WwwAuthChallengeState(enableWwwAuth));

// Register user context service (scoped to handle per-request user identity)
// Use DevUserContext when auth is not configured (allows dev user switching)
if (!authConfigured)
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

// WWW-Authenticate challenge middleware (before auth so it can short-circuit)
app.UseWwwAuthChallenge();

// Authentication & Authorization middleware (order matters)
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
