using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class DataProtectionRestartTests
{
    [Fact]
    public void Opaque_bearer_should_remain_valid_after_api_provider_recreation()
    {
        WithKeyDirectory(path =>
        {
            using var first = ApiServices(path);
            var ticket = Ticket("api-user", DateTimeOffset.UtcNow.AddHours(1), "Bearer");
            var token = BearerOptions(first).BearerTokenProtector.Protect(ticket);

            using var restarted = ApiServices(path);
            var restored = BearerOptions(restarted).BearerTokenProtector.Unprotect(token);

            restored.ShouldNotBeNull();
            restored.Principal.Identity!.Name.ShouldBe("api-user");
        });
    }

    [Fact]
    public void Protected_web_cookie_should_restore_api_credential_after_web_provider_recreation()
    {
        WithKeyDirectory(path =>
        {
            using var first = WebServices(path);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "owner"),
                new Claim(AuthConstants.SessionIdClaim, "session"),
                new Claim(AuthConstants.ApiAccessTokenClaim, "opaque-api-token")
            ], "cookie"));
            var protectedCookie = CookieOptions(first).TicketDataFormat.Protect(
                new AuthenticationTicket(principal, new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                    AllowRefresh = false
                }, AuthConstants.CookieScheme));

            using var restarted = WebServices(path);
            var restored = CookieOptions(restarted).TicketDataFormat.Unprotect(protectedCookie);

            restored.ShouldNotBeNull();
            WebAuthenticationEndpoints.HasApiCredential(
                restored.Principal,
                new ServerApiSessionStore()).ShouldBeTrue();
        });
    }

    [Fact]
    public void Invalid_token_should_not_be_accepted_after_restart()
    {
        WithKeyDirectory(path =>
        {
            using var services = ApiServices(path);
            BearerOptions(services).BearerTokenProtector.Unprotect("not-a-protected-token").ShouldBeNull();
        });
    }

    [Fact]
    public void Token_expiration_should_not_be_extended_by_restart()
    {
        WithKeyDirectory(path =>
        {
            var expiration = DateTimeOffset.UtcNow.AddMinutes(15);
            using var first = ApiServices(path);
            var token = BearerOptions(first).BearerTokenProtector.Protect(Ticket("user", expiration, "Bearer"));

            using var restarted = ApiServices(path);
            var restored = BearerOptions(restarted).BearerTokenProtector.Unprotect(token)!;

            restored.Properties.ExpiresUtc!.Value.ToUnixTimeSeconds().ShouldBe(expiration.ToUnixTimeSeconds());
        });
    }

    private static ServiceProvider ApiServices(string path)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection()
            .SetApplicationName("SBGarage.Api.RestartTest")
            .PersistKeysToFileSystem(new DirectoryInfo(path));
        services.AddAuthentication().AddBearerToken("Bearer");
        return services.BuildServiceProvider();
    }

    private static ServiceProvider WebServices(string path)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection()
            .SetApplicationName("SBGarage.Web.RestartTest")
            .PersistKeysToFileSystem(new DirectoryInfo(path));
        services.AddAuthentication().AddCookie(AuthConstants.CookieScheme);
        return services.BuildServiceProvider();
    }

    private static BearerTokenOptions BearerOptions(IServiceProvider services) =>
        services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get("Bearer");

    private static CookieAuthenticationOptions CookieOptions(IServiceProvider services) =>
        services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(AuthConstants.CookieScheme);

    private static AuthenticationTicket Ticket(string name, DateTimeOffset expiresAt, string scheme) =>
        new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], scheme)),
            new AuthenticationProperties { ExpiresUtc = expiresAt },
            scheme);

    private static void WithKeyDirectory(Action<string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sbgarage-dp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        try
        {
            test(path);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
