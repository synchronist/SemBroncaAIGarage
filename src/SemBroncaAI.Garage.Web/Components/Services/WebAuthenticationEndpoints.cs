using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public static class WebAuthenticationEndpoints
{
    public const string LoginRateLimitPolicy = "web-login";

    public static IEndpointRouteBuilder MapWebAuthentication(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync)
            .RequireRateLimiting(LoginRateLimitPolicy);
        endpoints.MapPost("/auth/logout", LogoutAsync);
        endpoints.MapGet("/auth/me", MeAsync).RequireAuthorization();
        endpoints.MapGet("/auth/garage-logo", GarageLogoAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IHttpClientFactory httpClientFactory,
        IServerApiSessionStore sessionStore)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var identifier = form["identifier"].ToString();
        var password = form["password"].ToString();
        var rememberMe = string.Equals(form["rememberMe"], "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password))
            return Results.LocalRedirect("/login?error=invalid");

        var client = httpClientFactory.CreateClient("AuthenticationApi");
        using var loginResponse = await client.PostAsJsonAsync(
            "api/auth/login",
            new { identifier, password },
            context.RequestAborted);

        if (!loginResponse.IsSuccessStatusCode)
        {
            var error = loginResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                ? "limited"
                : "invalid";
            return Results.LocalRedirect($"/login?error={error}");
        }

        var token = await loginResponse.Content.ReadFromJsonAsync<ApiTokenResponse>(
            cancellationToken: context.RequestAborted);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            return Results.LocalRedirect("/login?error=unavailable");

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        meRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var meResponse = await client.SendAsync(meRequest, context.RequestAborted);
        if (!meResponse.IsSuccessStatusCode)
            return Results.LocalRedirect("/login?error=invalid");

        var user = await meResponse.Content.ReadFromJsonAsync<CurrentUserModel>(
            cancellationToken: context.RequestAborted);
        if (user is null)
            return Results.LocalRedirect("/login?error=unavailable");

        var sessionId = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        sessionStore.Set(sessionId, new ApiSession(token.AccessToken, expiresAt, user));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(AuthConstants.SessionIdClaim, sessionId)
        };
        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        if (user.GarageId is not null)
            claims.Add(new Claim("garage_id", user.GarageId.Value.ToString()));
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var properties = CreateCookieProperties(rememberMe, expiresAt);
        await context.SignInAsync(
            AuthConstants.CookieScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConstants.CookieScheme)),
            properties);

        var destination = user.GarageId is null && user.Roles.Contains("PlatformAdmin")
            ? "/platform-admin"
            : "/";
        return Results.LocalRedirect(destination);
    }

    public static AuthenticationProperties CreateCookieProperties(
        bool rememberMe,
        DateTimeOffset expiresAt) => new()
    {
        IsPersistent = rememberMe,
        AllowRefresh = true,
        ExpiresUtc = expiresAt
    };

    private static async Task<IResult> MeAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IServerApiSessionStore sessionStore)
    {
        var sessionId = context.User.FindFirstValue(AuthConstants.SessionIdClaim);
        if (sessionId is null || !sessionStore.TryGet(sessionId, out var session))
            return Results.Unauthorized();

        var client = httpClientFactory.CreateClient("AuthenticationApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session!.AccessToken);
        using var response = await client.SendAsync(request, context.RequestAborted);
        if (!response.IsSuccessStatusCode)
            return Results.Unauthorized();

        var user = await response.Content.ReadFromJsonAsync<CurrentUserModel>(
            cancellationToken: context.RequestAborted);
        return user is null ? Results.Unauthorized() : Results.Json(user);
    }

    private static async Task<IResult> GarageLogoAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IServerApiSessionStore sessionStore)
    {
        var accessToken = ResolveApiAccessToken(context.User, sessionStore);

        if (accessToken is null)
            return Results.Unauthorized();

        var client = httpClientFactory.CreateClient("AuthenticationApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/garage/logo");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, context.RequestAborted);
        if (!response.IsSuccessStatusCode)
            return response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? Results.NotFound()
                : Results.Unauthorized();

        var bytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
        return Results.File(bytes, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    public static string? ResolveApiAccessToken(
        ClaimsPrincipal principal,
        IServerApiSessionStore sessionStore)
    {
        var accessToken = principal.FindFirstValue(AuthConstants.ApiAccessTokenClaim);
        if (accessToken is not null) return accessToken;

        var sessionId = principal.FindFirstValue(AuthConstants.SessionIdClaim);
        return sessionId is not null && sessionStore.TryGet(sessionId, out var session)
            ? session!.AccessToken
            : null;
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IServerApiSessionStore sessionStore)
    {
        await antiforgery.ValidateRequestAsync(context);
        var sessionId = context.User.FindFirstValue(AuthConstants.SessionIdClaim);
        if (sessionId is not null)
            sessionStore.Remove(sessionId);

        await context.SignOutAsync(AuthConstants.CookieScheme);
        return Results.LocalRedirect("/login");
    }
}
