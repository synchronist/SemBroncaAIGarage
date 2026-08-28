using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
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

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            ApiErrorResponse? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken);
            }
            catch (JsonException)
            {
                // Preserve unrelated forbidden responses that do not use the API error contract.
            }
            catch (NotSupportedException)
            {
                // Preserve unrelated forbidden responses with a non-JSON content type.
            }

            if (string.Equals(error?.Code, "subscription-restricted", StringComparison.Ordinal))
            {
                response.Dispose();
                throw new HttpRequestException(
                    error?.Message ?? "A assinatura da oficina precisa ser regularizada para continuar.",
                    inner: null,
                    HttpStatusCode.Forbidden);
            }
        }

        return response;
    }

    private sealed record ApiErrorResponse(string? Code, string? Message);
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
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        var baseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("A URL da API não foi configurada.");
        var authenticationHandler = new ApiAuthenticationHandler(
            authenticationStateProvider,
            sessionStore)
        {
            InnerHandler = new CorrelationIdHandler(httpContextAccessor)
            {
                InnerHandler = new HttpClientHandler()
            }
        };
        Client = new HttpClient(authenticationHandler)
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public HttpClient Client { get; }

    public void Dispose() => Client.Dispose();
}
