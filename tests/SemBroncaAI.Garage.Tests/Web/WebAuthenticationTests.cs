using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

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
    public void Me_contract_should_be_protected_and_expose_only_safe_fields()
    {
        typeof(AuthController).GetMethod(nameof(AuthController.Me))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe("ActiveUser");
        typeof(CurrentUserResponse).GetProperties().Select(property => property.Name)
            .ShouldBe(["UserId", "Name", "Email", "Username", "GarageId", "Roles"], ignoreOrder: true);
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
}
