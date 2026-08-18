using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class ApiBearerBridgeHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHttpClientFactory httpClientFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiBearerBridge";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token)) return AuthenticateResult.Fail("Bearer ausente.");

        var client = httpClientFactory.CreateClient("AuthenticationApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, Context.RequestAborted);
        if (!response.IsSuccessStatusCode) return AuthenticateResult.Fail("Bearer inválido.");
        var user = await response.Content.ReadFromJsonAsync<CurrentUserModel>(cancellationToken: Context.RequestAborted);
        if (user is null) return AuthenticateResult.Fail("Usuário inválido.");

        var claims = ApiBearerBridgeClaims.Create(user, token);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}

public static class ApiBearerBridgeClaims
{
    public static IReadOnlyCollection<Claim> Create(CurrentUserModel user, string token)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(AuthConstants.ApiAccessTokenClaim, token)
        };
        if (user.GarageId is not null) claims.Add(new Claim("garage_id", user.GarageId.Value.ToString()));
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(user.EffectivePermissions.Select(permission =>
            new Claim(ApplicationPermissions.ClaimType, permission)));
        return claims;
    }
}
