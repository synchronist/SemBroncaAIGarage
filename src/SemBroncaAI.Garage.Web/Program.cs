using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;
using SemBroncaAI.Garage.Web.Components;
using SemBroncaAI.Garage.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();

builder.Services.AddAuthentication(AuthConstants.CookieScheme)
    .AddCookie(AuthConstants.CookieScheme, options =>
    {
        options.Cookie.Name = builder.Environment.IsDevelopment()
            ? "SBGarage.Session"
            : "__Host-SBGarage.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = context =>
        {
            var sessionId = context.Principal?.FindFirst(AuthConstants.SessionIdClaim)?.Value;
            var store = context.HttpContext.RequestServices.GetRequiredService<IServerApiSessionStore>();
            if (sessionId is null || !store.TryGet(sessionId, out _))
                context.RejectPrincipal();
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/auth/me"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IServerApiSessionStore, ServerApiSessionStore>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("A URL da API não foi configurada.");
builder.Services.AddHttpClient("AuthenticationApi", client =>
    client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(WebAuthenticationEndpoints.LoginRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddScoped<AuthenticatedApiHttpClient>();
builder.Services.AddScoped<LookupService>(CreateAuthenticatedService<LookupService>);
builder.Services.AddScoped<ServiceOrderService>(CreateAuthenticatedService<ServiceOrderService>);
builder.Services.AddScoped<CustomerService>(CreateAuthenticatedService<CustomerService>);
builder.Services.AddScoped<VehicleService>(CreateAuthenticatedService<VehicleService>);
builder.Services.AddScoped<GarageService>(CreateAuthenticatedService<GarageService>);
builder.Services.AddScoped<EstimateService>(CreateAuthenticatedService<EstimateService>);
builder.Services.AddHttpClient<PublicApprovalService>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<WhatsAppShareBuilder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapWebAuthentication();

app.Run();

TClient CreateAuthenticatedService<TClient>(IServiceProvider serviceProvider) where TClient : class =>
    ActivatorUtilities.CreateInstance<TClient>(
        serviceProvider,
        serviceProvider.GetRequiredService<AuthenticatedApiHttpClient>().Client);
