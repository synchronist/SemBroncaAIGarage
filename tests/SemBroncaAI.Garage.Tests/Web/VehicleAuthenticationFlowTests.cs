using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class VehicleAuthenticationFlowTests
{
    [Fact]
    public async Task Create_Should_Propagate_Server_Session_Bearer_To_Api()
    {
        const string sessionId = "session-1";
        const string accessToken = "access-token";
        var store = Store(sessionId, accessToken);
        var handler = new ApiAuthenticationHandler(AuthenticationState(sessionId), store)
        {
            InnerHandler = new CallbackHandler(request =>
            {
                request.Headers.Authorization?.Scheme.ShouldBe("Bearer");
                request.Headers.Authorization?.Parameter.ShouldBe(accessToken);
                return Response(HttpStatusCode.Created, SuccessJson);
            })
        };
        var service = Service(handler);

        var result = await service.CreateAsync(Request());

        result.Plate.ShouldBe("ABC1D23");
    }

    [Fact]
    public async Task Create_Should_Invalidate_Stale_Session_And_Show_Friendly_Message_On_401()
    {
        const string sessionId = "session-2";
        var store = Store(sessionId, "stale-token");
        var handler = new ApiAuthenticationHandler(AuthenticationState(sessionId), store)
        {
            InnerHandler = new CallbackHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))
        };
        var service = Service(handler);

        var exception = await Should.ThrowAsync<HttpRequestException>(() => service.CreateAsync(Request()));

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        exception.Message.ShouldBe("Sua sessão expirou. Entre novamente para continuar.");
        store.TryGet(sessionId, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "Você não tem permissão para executar esta ação.")]
    [InlineData(HttpStatusCode.InternalServerError, "Não foi possível salvar o veículo. Tente novamente em instantes.")]
    public async Task Create_Should_Not_Expose_Json_Error_For_Empty_Api_Response(
        HttpStatusCode statusCode,
        string expectedMessage)
    {
        var service = Service(new CallbackHandler(_ => new HttpResponseMessage(statusCode)));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => service.CreateAsync(Request()));

        exception.Message.ShouldBe(expectedMessage);
        exception.Message.ShouldNotContain("JSON", Case.Insensitive);
    }

    private static VehicleService Service(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private static SaveVehicleRequest Request() =>
        new(Guid.CreateVersion7(), "ABC1D23", "Honda", "Civic", "EX", 2025, "Prata", "Flex", 10);

    private static ServerApiSessionStore Store(string sessionId, string accessToken)
    {
        var store = new ServerApiSessionStore();
        store.Set(sessionId, new ApiSession(
            accessToken,
            DateTimeOffset.UtcNow.AddHours(1),
            new CurrentUserModel(Guid.CreateVersion7(), "Owner", "owner@test.local", "owner", Guid.CreateVersion7(), ["Owner"])));
        return store;
    }

    private static AuthenticationStateProvider AuthenticationState(string sessionId) =>
        new TestAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthConstants.SessionIdClaim, sessionId)],
            "test")));

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private const string SuccessJson =
        """{"id":"019fb7ee-1479-7f7b-95a1-496112008629","garageId":"019fb7ee-1479-7f7b-95a1-496112008630","customerId":"019fb7ee-1479-7f7b-95a1-496112008631","plate":"ABC1D23","brand":"Honda","model":"Civic","version":"EX","year":2025,"color":"Prata","fuel":"Flex","mileage":10,"active":true,"createdAt":"2026-08-29T00:00:00Z"}""";

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class CallbackHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(callback(request));
    }
}
