using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Web.Services;

public static class WebAuthenticationEndpoints
{
    public const string LoginRateLimitPolicy = "web-login";
    public const string PasswordRecoveryRateLimitPolicy = "web-password-recovery";

    public static IEndpointRouteBuilder MapWebAuthentication(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync)
            .RequireRateLimiting(LoginRateLimitPolicy);
        endpoints.MapPost("/auth/logout", LogoutAsync);
        endpoints.MapGet("/auth/me", MeAsync).RequireAuthorization();
        endpoints.MapGet("/auth/garage-logo", GarageLogoAsync).RequireAuthorization();
        endpoints.MapPost("/auth/forgot-password", ForgotPasswordAsync)
            .RequireRateLimiting(PasswordRecoveryRateLimitPolicy);
        endpoints.MapPost("/auth/reset-password", ResetPasswordAsync)
            .RequireRateLimiting(PasswordRecoveryRateLimitPolicy);
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
        var returnUrl = form["returnUrl"].ToString();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password))
            return Results.LocalRedirect("/login?error=invalid");

        var client = httpClientFactory.CreateClient("AuthenticationApi");
        HttpResponseMessage loginResponse;
        try
        {
            loginResponse = await client.PostAsJsonAsync(
                "api/auth/login",
                new { identifier, password },
                context.RequestAborted);
        }
        catch (Exception exception) when ((exception is HttpRequestException or TaskCanceledException) &&
                                          !context.RequestAborted.IsCancellationRequested)
        {
            return Results.LocalRedirect("/login?error=unavailable");
        }

        using (loginResponse)
        {
            if (!loginResponse.IsSuccessStatusCode)
            {
                var error = await ResolveLoginErrorAsync(loginResponse, context.RequestAborted);
                return Results.LocalRedirect($"/login?error={error}");
            }

            var token = await TryReadJsonAsync<ApiTokenResponse>(loginResponse.Content, context.RequestAborted);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                return Results.LocalRedirect("/login?error=unavailable");

            using var meRequest = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            meRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
            HttpResponseMessage meResponse;
            try
            {
                meResponse = await client.SendAsync(meRequest, context.RequestAborted);
            }
            catch (Exception exception) when ((exception is HttpRequestException or TaskCanceledException) &&
                                              !context.RequestAborted.IsCancellationRequested)
            {
                return Results.LocalRedirect("/login?error=unavailable");
            }
            using (meResponse)
            {
            if (!meResponse.IsSuccessStatusCode)
                return Results.LocalRedirect("/login?error=invalid");

            var user = await TryReadJsonAsync<CurrentUserModel>(meResponse.Content, context.RequestAborted);
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
            claims.AddRange(user.EffectivePermissions.Select(permission =>
                new Claim(ApplicationPermissions.ClaimType, permission)));

            var properties = CreateCookieProperties(rememberMe, expiresAt);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConstants.CookieScheme));
            await context.SignInAsync(
                AuthConstants.CookieScheme,
                principal,
                properties);

            var destination = AuthorizedLandingPage.Resolve(principal, returnUrl);
            return Results.LocalRedirect(destination);
            }
        }
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

        var user = await TryReadJsonAsync<CurrentUserModel>(response.Content, context.RequestAborted);
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

    private static async Task<IResult> ForgotPasswordAsync(
        HttpContext context, IAntiforgery antiforgery, IHttpClientFactory httpClientFactory)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var client = httpClientFactory.CreateClient("AuthenticationApi");
        try
        {
            using var response = await client.PostAsJsonAsync("api/auth/password/forgot", new { email = form["email"].ToString() }, context.RequestAborted);
            return response.IsSuccessStatusCode
                ? Results.LocalRedirect("/forgot-password?sent=true")
                : Results.LocalRedirect("/forgot-password?error=unavailable");
        }
        catch (HttpRequestException)
        {
            return Results.LocalRedirect("/forgot-password?error=unavailable");
        }
    }

    private static async Task<IResult> ResetPasswordAsync(
        HttpContext context, IAntiforgery antiforgery, IHttpClientFactory httpClientFactory)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var encodedToken = form["token"].ToString();
        if (!Guid.TryParse(form["userId"], out var userId))
            return Results.LocalRedirect("/reset-password?error=invalid");

        var password = form["password"].ToString();
        var confirmation = form["confirmPassword"].ToString();
        var resetPage = $"/reset-password?userId={userId:D}&token={Uri.EscapeDataString(encodedToken)}";
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            return Results.LocalRedirect($"{resetPage}&error=mismatch");

        var client = httpClientFactory.CreateClient("AuthenticationApi");
        using var response = await client.PostAsJsonAsync("api/auth/password/reset", new
        {
            userId,
            token = encodedToken,
            password,
            confirmPassword = confirmation
        }, context.RequestAborted);
        if (response.IsSuccessStatusCode) return Results.LocalRedirect("/login?reset=true");
        var error = await TryReadJsonAsync<PasswordResetErrorModel>(response.Content, context.RequestAborted);
        var reason = error?.Code is "password" or "same-password" or "mismatch" ? error.Code : "invalid";
        return reason == "invalid"
            ? Results.LocalRedirect("/reset-password?error=invalid")
            : Results.LocalRedirect($"{resetPage}&error={Uri.EscapeDataString(reason)}");
    }

    public static async Task<T?> TryReadJsonAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
    {
        var mediaType = content.Headers.ContentType?.MediaType;
        if (mediaType is null ||
            !(mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
              mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)))
            return default;
        try
        {
            return await content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or NotSupportedException)
        {
            return default;
        }
    }

    public static async Task<string> ResolveLoginErrorAsync(
        HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            return "limited";
        if ((int)response.StatusCode == StatusCodes.Status423Locked)
            return "locked";
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return AuthenticationErrorCodes.GarageInactive;
        if ((int)response.StatusCode >= 500)
            return "unavailable";

        var error = await TryReadJsonAsync<AuthErrorModel>(response.Content, cancellationToken);
        return error?.Code == AuthenticationErrorCodes.GarageInactive
            ? AuthenticationErrorCodes.GarageInactive
            : "invalid";
    }
}
