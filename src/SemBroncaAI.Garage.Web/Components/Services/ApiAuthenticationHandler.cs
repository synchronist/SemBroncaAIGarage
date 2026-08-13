using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class ApiAuthenticationHandler(
    AuthenticationStateProvider authenticationStateProvider,
    IServerApiSessionStore sessionStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var sessionId = authenticationState.User.FindFirstValue(AuthConstants.SessionIdClaim);

        if (sessionId is not null && sessionStore.TryGet(sessionId, out var session))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        }
        else if (authenticationState.User.FindFirstValue(AuthConstants.ApiAccessTokenClaim) is { } accessToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

public static class AuthConstants
{
    public const string CookieScheme = "SBGarage.Web";
    public const string SessionIdClaim = "sbg:session_id";
    public const string ApiAccessTokenClaim = "sbg:api_access_token";
}

public sealed class AuthenticatedApiHttpClient : IDisposable
{
    public AuthenticatedApiHttpClient(
        AuthenticationStateProvider authenticationStateProvider,
        IServerApiSessionStore sessionStore,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("A URL da API não foi configurada.");
        var authenticationHandler = new ApiAuthenticationHandler(
            authenticationStateProvider,
            sessionStore)
        {
            InnerHandler = new HttpClientHandler()
        };
        Client = new HttpClient(authenticationHandler)
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public HttpClient Client { get; }

    public void Dispose() => Client.Dispose();
}
