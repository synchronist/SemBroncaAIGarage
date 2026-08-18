using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using MudBlazor.Services;
using SemBroncaAI.Garage.Web.Components;
using SemBroncaAI.Garage.Web.Services;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
WebDeploymentConfiguration.Configure(builder);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddHealthChecks().AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "SBGarage.Dynamic";
        options.DefaultChallengeScheme = AuthConstants.CookieScheme;
    })
    .AddPolicyScheme("SBGarage.Dynamic", null, options =>
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? ApiBearerBridgeHandler.SchemeName
                : AuthConstants.CookieScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiBearerBridgeHandler>(ApiBearerBridgeHandler.SchemeName, null)
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
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in ApplicationPermissions.All)
        options.AddPolicy(permission, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("garage_id")
            .RequireClaim(ApplicationPermissions.ClaimType, permission));
    options.AddPolicy("TenantUser", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("garage_id")
        .RequireAssertion(context => !context.User.IsInRole("PlatformAdmin")));
    options.AddPolicy(PlatformAuthorization.Policy, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole("PlatformAdmin")
        .RequireAssertion(context => !context.User.HasClaim(claim => claim.Type == "garage_id")));
});
builder.Services.AddSingleton<IServerApiSessionStore, ServerApiSessionStore>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("A URL da API não foi configurada.");
builder.Services.AddHttpClient("AuthenticationApi", client =>
    client.BaseAddress = new Uri(apiBaseUrl)).AddHttpMessageHandler<CorrelationIdHandler>();
builder.Services.AddHttpClient<PlatformHealthService>(client =>
    client.BaseAddress = new Uri(apiBaseUrl)).AddHttpMessageHandler<CorrelationIdHandler>();

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
    options.AddPolicy(WebAuthenticationEndpoints.PasswordRecoveryRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
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
builder.Services.AddScoped<PlatformGarageService>(CreateAuthenticatedService<PlatformGarageService>);
builder.Services.AddScoped<TeamService>(CreateAuthenticatedService<TeamService>);
builder.Services.AddHttpClient<PublicApprovalService>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<TeamInvitationService>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<WhatsAppShareBuilder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<WebOperationalMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapWebAuthentication();
app.MapHealthChecks("/health/live", new() { ResponseWriter = MinimalHealthResponse }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new() { ResponseWriter = MinimalHealthResponse }).AllowAnonymous();

app.Run();

static Task MinimalHealthResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
}

TClient CreateAuthenticatedService<TClient>(IServiceProvider serviceProvider) where TClient : class =>
    ActivatorUtilities.CreateInstance<TClient>(
        serviceProvider,
        serviceProvider.GetRequiredService<AuthenticatedApiHttpClient>().Client);
