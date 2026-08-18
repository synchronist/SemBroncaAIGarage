using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Infrastructure.Identity;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class PasswordRecoveryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task Forgot_should_have_neutral_behavior_and_only_send_for_active_confirmed_user(bool exists, bool active)
    {
        var gateway = new Gateway();
        if (exists)
        {
            gateway.User = ApplicationUser.CreateGarageUser("User", "user@test.local", "user", Guid.NewGuid());
            gateway.User.EmailConfirmed = true;
            if (!active) gateway.User.Deactivate();
        }
        var sender = new Sender();
        var service = CreateService(gateway, sender);

        await service.RequestAsync("user@test.local", default);

        sender.Links.Count.ShouldBe(exists && active ? 1 : 0);
        gateway.GeneratedTokens.ShouldBe(exists && active ? 1 : 0);
    }

    [Fact]
    public async Task Disabled_recovery_should_remain_neutral_without_looking_up_user_or_generating_token()
    {
        var gateway = new Gateway();
        var sender = new Sender();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["PasswordRecovery:Enabled"] = "false" }).Build();

        await new PasswordRecoveryService(gateway, sender, configuration)
            .RequestAsync("known@test.local", default);

        gateway.EmailLookups.ShouldBe(0);
        gateway.GeneratedTokens.ShouldBe(0);
        sender.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task Valid_reset_should_use_identity_token_update_stamp_and_prevent_reuse()
    {
        var gateway = ValidGateway();
        var service = CreateService(gateway, new Sender());
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes("valid-token"));

        (await service.ResetAsync(gateway.User!.Id, encoded, "NewPassword123", "NewPassword123"))
            .ShouldBe(PasswordResetResult.Success);
        gateway.StampUpdates.ShouldBe(1);
        (await service.ResetAsync(gateway.User.Id, encoded, "NewPassword123", "NewPassword123"))
            .ShouldBe(PasswordResetResult.Invalid);
    }

    [Theory]
    [InlineData("bad-token", "NewPassword123", "NewPassword123", PasswordResetStatus.Invalid)]
    [InlineData("valid-token", "short", "short", PasswordResetStatus.PasswordRejected)]
    [InlineData("valid-token", "NewPassword123", "Different123", PasswordResetStatus.Mismatch)]
    public async Task Invalid_reset_scenarios_should_be_safe(string token, string password, string confirmation, PasswordResetStatus expected)
    {
        var gateway = ValidGateway();
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
        (await CreateService(gateway, new Sender()).ResetAsync(gateway.User!.Id, encoded, password, confirmation)).Status.ShouldBe(expected);
    }

    [Fact]
    public async Task Mismatched_confirmation_should_not_call_identity_or_consume_token()
    {
        var gateway = ValidGateway(); var service = CreateService(gateway, new Sender()); var token = EncodedToken();
        (await service.ResetAsync(gateway.User!.Id, token, "NewPassword123", "Different123")).Status.ShouldBe(PasswordResetStatus.Mismatch);
        gateway.ResetAttempts.ShouldBe(0);
        (await service.ResetAsync(gateway.User.Id, token, "NewPassword123", "NewPassword123")).Status.ShouldBe(PasswordResetStatus.Success);
    }

    [Fact]
    public async Task Current_password_should_be_rejected_without_consuming_token()
    {
        var gateway = ValidGateway(); var service = CreateService(gateway, new Sender()); var token = EncodedToken();
        (await service.ResetAsync(gateway.User!.Id, token, gateway.CurrentPassword, gateway.CurrentPassword)).Status.ShouldBe(PasswordResetStatus.SamePassword);
        gateway.ResetAttempts.ShouldBe(0);
        (await service.ResetAsync(gateway.User.Id, token, "DifferentPassword123", "DifferentPassword123")).Status.ShouldBe(PasswordResetStatus.Success);
    }

    [Theory]
    [InlineData("PasswordTooShort", "A senha deve ter pelo menos 10 caracteres.")]
    [InlineData("PasswordRequiresUpper", "Adicione pelo menos uma letra maiúscula.")]
    [InlineData("PasswordRequiresLower", "Adicione pelo menos uma letra minúscula.")]
    [InlineData("PasswordRequiresDigit", "Adicione pelo menos um número.")]
    [InlineData("PasswordRequiresUniqueChars", "Use pelo menos quatro caracteres diferentes.")]
    public void Identity_password_errors_should_have_friendly_portuguese_messages(string code, string expected) =>
        PasswordPolicyMessages.From([new IdentityError { Code = code, Description = "technical identity message" }])
            .ShouldBe([expected]);

    [Fact]
    public void Recovery_endpoints_should_be_anonymous_and_rate_limited()
    {
        typeof(PasswordRecoveryController).GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
        foreach (var method in new[] { nameof(PasswordRecoveryController.Forgot), nameof(PasswordRecoveryController.Reset) })
            typeof(PasswordRecoveryController).GetMethod(method)!.GetCustomAttribute<EnableRateLimitingAttribute>()!
                .PolicyName.ShouldBe(AuthenticationRateLimiting.PasswordRecoveryPolicy);
    }

    private static PasswordRecoveryService CreateService(Gateway gateway, Sender sender) => new(
        gateway, sender, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PasswordRecovery:Enabled"] = "true",
                ["Web:BaseUrl"] = "http://localhost:5123/"
            }).Build());

    private static Gateway ValidGateway()
    {
        var gateway = new Gateway { User = ApplicationUser.CreateGarageUser("User", "user@test.local", "user", Guid.NewGuid()) };
        gateway.User.EmailConfirmed = true;
        return gateway;
    }

    private static string EncodedToken() => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
        System.Text.Encoding.UTF8.GetBytes("valid-token"));

    private sealed class Sender : IPasswordResetEmailSender
    {
        public List<string> Links { get; } = [];
        public Task SendAsync(string email, string resetLink, CancellationToken cancellationToken) { Links.Add(resetLink); return Task.CompletedTask; }
    }

    private sealed class Gateway : IPasswordRecoveryGateway
    {
        public ApplicationUser? User { get; set; }
        public int GeneratedTokens { get; private set; }
        public int StampUpdates { get; private set; }
        public int ResetAttempts { get; private set; }
        public int EmailLookups { get; private set; }
        public string CurrentPassword { get; set; } = "CurrentPassword123";
        private bool _used;
        public Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            EmailLookups++;
            return Task.FromResult(User?.Email == email ? User : null);
        }
        public Task<ApplicationUser?> FindByIdAsync(Guid userId) => Task.FromResult(User?.Id == userId ? User : null);
        public Task<string> GenerateTokenAsync(ApplicationUser user) { GeneratedTokens++; return Task.FromResult("valid-token"); }
        public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) => Task.FromResult(password == CurrentPassword);
        public Task<bool> VerifyTokenAsync(ApplicationUser user, string token) => Task.FromResult(token == "valid-token" && !_used);
        public Task<IdentityResult> ResetAsync(ApplicationUser user, string token, string password)
        {
            ResetAttempts++;
            if (password.Length < 10) return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort" }));
            if (token != "valid-token" || _used) return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "InvalidToken" }));
            _used = true; CurrentPassword = password; return Task.FromResult(IdentityResult.Success);
        }
        public Task UpdateSecurityStampAsync(ApplicationUser user) { StampUpdates++; return Task.CompletedTask; }
    }
}
