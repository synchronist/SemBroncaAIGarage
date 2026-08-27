using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using System.Security.Claims;
using Shouldly;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class WebAuthenticationTests
{
    [Fact]
    public void Remember_me_false_should_create_non_persistent_cookie()
    {
        var properties = WebAuthenticationEndpoints.CreateCookieProperties(false, DateTimeOffset.UtcNow.AddDays(7));
        properties.IsPersistent.ShouldBeFalse();
        properties.AllowRefresh.ShouldBe(true);
    }

    [Fact]
    public void Remember_me_true_should_create_persistent_cookie()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var properties = WebAuthenticationEndpoints.CreateCookieProperties(true, expiresAt);
        properties.IsPersistent.ShouldBeTrue();
        properties.ExpiresUtc!.Value.ToUnixTimeSeconds().ShouldBe(expiresAt.ToUnixTimeSeconds());
    }

    [Fact]
    public void Logout_should_make_server_side_credential_unavailable()
    {
        var store = new ServerApiSessionStore();
        var session = new ApiSession("secret", DateTimeOffset.UtcNow.AddHours(1),
            new CurrentUserModel(Guid.NewGuid(), "Owner", null, "owner", Guid.NewGuid(), ["Owner"]));
        store.Set("session", session);
        store.TryGet("session", out _).ShouldBeTrue();
        store.Remove("session");
        store.TryGet("session", out _).ShouldBeFalse();
    }

    [Fact]
    public void Expired_api_credential_should_not_be_reused()
    {
        var store = new ServerApiSessionStore();
        store.Set("expired", new ApiSession("secret", DateTimeOffset.UtcNow.AddSeconds(-1),
            new CurrentUserModel(Guid.NewGuid(), "Owner", null, null, Guid.NewGuid(), [])));
        store.TryGet("expired", out _).ShouldBeFalse();
    }

    [Fact]
    public void Adding_a_session_should_sweep_other_expired_sessions()
    {
        var store = new ServerApiSessionStore();
        var user = new CurrentUserModel(Guid.NewGuid(), "User", null, "user", Guid.NewGuid(), []);
        store.Set("expired", new ApiSession("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1), user));

        store.Set("current", new ApiSession("current-token", DateTimeOffset.UtcNow.AddMinutes(5), user));

        store.TryGet("expired", out _).ShouldBeFalse();
        store.TryGet("current", out _).ShouldBeTrue();
    }

    [Fact]
    public void Me_contract_should_be_protected_and_expose_only_safe_fields()
    {
        typeof(AuthController).GetMethod(nameof(AuthController.Me))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe("ActiveUser");
        typeof(CurrentUserResponse).GetProperties().Select(property => property.Name)
            .ShouldBe(["UserId", "Name", "Email", "Username", "GarageId", "Roles", "Permissions"], ignoreOrder: true);
        typeof(CurrentUserResponse).GetProperties().Select(property => property.Name)
            .ShouldNotContain(name => name.Contains("Password") || name.Contains("Stamp") || name.Contains("Token"));
    }

    [Fact]
    public void Login_should_be_rate_limited_and_public_approval_should_remain_anonymous()
    {
        typeof(AuthController).GetMethod(nameof(AuthController.Login))!
            .GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName
            .ShouldBe("login");

        typeof(PublicApprovalsController).GetCustomAttribute<AuthorizeAttribute>()
            .ShouldBeNull();
        foreach (var method in typeof(PublicApprovalsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.DeclaringType == typeof(PublicApprovalsController)))
        {
            method.GetCustomAttribute<AuthorizeAttribute>().ShouldBeNull();
        }
    }

    [Fact]
    public void Authenticated_logo_should_use_server_session_or_validated_playwright_bridge()
    {
        var store = new ServerApiSessionStore();
        store.Set("browser-session", new ApiSession("browser-token", DateTimeOffset.UtcNow.AddMinutes(5),
            new CurrentUserModel(Guid.NewGuid(), "Owner", null, "owner", Guid.NewGuid(), ["Owner"])));
        var browser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthConstants.SessionIdClaim, "browser-session")], "cookie"));
        WebAuthenticationEndpoints.ResolveApiAccessToken(browser, store).ShouldBe("browser-token");

        var playwright = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthConstants.ApiAccessTokenClaim, "validated-bridge-token")], "bridge"));
        WebAuthenticationEndpoints.ResolveApiAccessToken(playwright, store).ShouldBe("validated-bridge-token");
    }

    [Fact]
    public void Logo_should_not_be_available_without_authenticated_server_credential()
    {
        WebAuthenticationEndpoints.ResolveApiAccessToken(
            new ClaimsPrincipal(new ClaimsIdentity()),
            new ServerApiSessionStore()).ShouldBeNull();
    }

    [Fact]
    public async Task Json_reader_should_preserve_known_json_and_ignore_html_or_invalid_json()
    {
        using var valid = Content("{\"code\":\"same-password\"}", "application/json");
        (await WebAuthenticationEndpoints.TryReadJsonAsync<PasswordResetErrorModel>(valid))!.Code.ShouldBe("same-password");

        using var html = Content("<html>proxy failure</html>", "text/html");
        (await WebAuthenticationEndpoints.TryReadJsonAsync<PasswordResetErrorModel>(html)).ShouldBeNull();

        using var invalid = Content("not-json", "application/json");
        (await WebAuthenticationEndpoints.TryReadJsonAsync<PasswordResetErrorModel>(invalid)).ShouldBeNull();
    }

    [Fact]
    public async Task Inactive_garage_code_should_reach_the_login_message()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = Content("{\"message\":\"safe\",\"code\":\"garage-inactive\"}", "application/json")
        };

        var error = await WebAuthenticationEndpoints.ResolveLoginErrorAsync(response);

        error.ShouldBe(AuthenticationErrorCodes.GarageInactive);
        LoginErrorMessages.Resolve(error)
            .ShouldBe("O acesso desta oficina está temporariamente indisponível.");
    }

    [Fact]
    public async Task Inactive_garage_status_should_survive_even_when_an_intermediary_removes_the_body()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden);

        var error = await WebAuthenticationEndpoints.ResolveLoginErrorAsync(response);

        error.ShouldBe(AuthenticationErrorCodes.GarageInactive);
        LoginErrorMessages.Resolve(error)
            .ShouldBe("O acesso desta oficina está temporariamente indisponível.");
    }

    [Fact]
    public async Task Generic_unauthorized_response_should_not_gain_specific_account_information()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = Content("{\"message\":\"invalid\"}", "application/json")
        };

        var error = await WebAuthenticationEndpoints.ResolveLoginErrorAsync(response);

        error.ShouldBe("invalid");
        LoginErrorMessages.Resolve(error)
            .ShouldBe("Não foi possível entrar com as credenciais informadas.");
    }

    [Theory]
    [InlineData(423, "locked")]
    [InlineData(429, "limited")]
    [InlineData(500, "unavailable")]
    public async Task Existing_login_failure_states_should_remain_distinct(int statusCode, string expected)
    {
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        (await WebAuthenticationEndpoints.ResolveLoginErrorAsync(response)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Owner", ApplicationPermissions.CreateServiceOrder, "/receive")]
    [InlineData("Receptionist", ApplicationPermissions.CreateServiceOrder, "/receive")]
    [InlineData("Mechanic", ApplicationPermissions.ViewServiceOrders, "/service-orders")]
    [InlineData("PlatformAdmin", null, "/platform-admin")]
    public void Landing_page_should_be_accessible_for_each_profile(string role, string? permission, string expected)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (permission is not null) claims.Add(new Claim(ApplicationPermissions.ClaimType, permission));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        AuthorizedLandingPage.For(user).ShouldBe(expected);
    }

    [Fact]
    public void Mechanic_return_url_should_only_be_preserved_when_authorized()
    {
        var user = Principal("Mechanic", ApplicationPermissions.ViewServiceOrders);

        AuthorizedLandingPage.Resolve(user, "/service-orders/00000000-0000-0000-0000-000000000001")
            .ShouldBe("/service-orders/00000000-0000-0000-0000-000000000001");
        AuthorizedLandingPage.Resolve(user, "/settings").ShouldBe("/service-orders");
        AuthorizedLandingPage.Resolve(user, "https://evil.example").ShouldBe("/service-orders");
    }

    [Fact]
    public void Subscription_return_url_should_only_be_preserved_for_owner_permission()
    {
        var owner = Principal("Owner", ApplicationPermissions.ViewSubscription);
        var receptionist = Principal("Receptionist", ApplicationPermissions.CreateServiceOrder);

        AuthorizedLandingPage.Resolve(owner, "/subscription").ShouldBe("/subscription");
        AuthorizedLandingPage.Resolve(receptionist, "/subscription").ShouldBe("/receive");
    }

    [Fact]
    public async Task Platform_onboarding_client_should_preserve_api_errors_by_field()
    {
        using var client = new HttpClient(new StaticResponseHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = Content("{\"message\":\"Revise os campos destacados abaixo.\",\"errors\":{\"ownerEmail\":[\"Informe um e-mail válido.\"]}}", "application/json")
        })) { BaseAddress = new Uri("http://localhost/") };
        var service = new PlatformGarageService(client);

        var exception = await Should.ThrowAsync<PlatformGarageFormValidationException>(() => service.CreateAsync(
            new CreatePlatformGarageCommand("Garage", "123", "15999999999", "garage@test.local", "Owner", "invalid", "owner")));

        exception.Errors["ownerEmail"].ShouldBe(["Informe um e-mail válido."]);
        exception.Message.ShouldBe("Revise os campos destacados abaixo.");
    }

    private static ClaimsPrincipal Principal(string role, params string[] permissions) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role), .. permissions.Select(permission => new Claim(ApplicationPermissions.ClaimType, permission))],
            "test"));

    private static StringContent Content(string value, string mediaType)
    {
        var content = new StringContent(value, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return content;
    }

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
