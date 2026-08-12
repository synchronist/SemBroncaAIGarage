using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Infrastructure.Identity;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class IdentityLoginServiceTests
{
    [Fact]
    public async Task Should_login_by_email()
    {
        var gateway = ValidGateway();
        var result = await Authenticate(gateway, "owner@test.local");
        result.Succeeded.ShouldBeTrue();
        gateway.EmailLookups.ShouldBe(1);
        gateway.NameLookups.ShouldBe(0);
    }

    [Fact]
    public async Task Should_login_by_username_when_email_does_not_match()
    {
        var gateway = ValidGateway();
        var result = await Authenticate(gateway, "owner");
        result.Succeeded.ShouldBeTrue();
        gateway.EmailLookups.ShouldBe(1);
        gateway.NameLookups.ShouldBe(1);
    }

    [Fact]
    public async Task Invalid_password_should_fail_generically_and_enable_identity_lockout_counting()
    {
        var gateway = ValidGateway();
        gateway.PasswordResult = SignInResult.Failed;
        var result = await Authenticate(gateway, "owner");
        result.Succeeded.ShouldBeFalse();
        result.IsLockedOut.ShouldBeFalse();
        gateway.PasswordChecks.ShouldBe(1);
    }

    [Fact]
    public async Task Missing_user_should_fail_without_revealing_lookup_used()
    {
        var gateway = ValidGateway(); gateway.User = null;
        var result = await Authenticate(gateway, "missing@test.local");
        result.ShouldBe(IdentityLoginResult.Failed);
    }

    [Fact]
    public async Task Inactive_user_should_not_be_authenticated()
    {
        var gateway = ValidGateway(); gateway.User!.Deactivate();
        var result = await Authenticate(gateway, "owner");
        result.Succeeded.ShouldBeFalse();
        gateway.PasswordChecks.ShouldBe(0);
    }

    [Fact]
    public async Task Locked_user_should_remain_locked_out()
    {
        var gateway = ValidGateway(); gateway.PasswordResult = SignInResult.LockedOut;
        var result = await Authenticate(gateway, "owner");
        result.Succeeded.ShouldBeFalse();
        result.IsLockedOut.ShouldBeTrue();
    }

    [Fact]
    public async Task Tenant_user_requires_its_existing_garage()
    {
        var gateway = ValidGateway(); gateway.GarageExists = false;
        var result = await Authenticate(gateway, "owner");
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Platform_admin_may_authenticate_only_without_garage()
    {
        var gateway = ValidGateway();
        gateway.User = ApplicationUser.CreatePlatformAdmin("Admin", "admin@test.local", "admin");
        gateway.Roles = [ApplicationRoles.PlatformAdmin];
        (await Authenticate(gateway, "admin")).Succeeded.ShouldBeTrue();

        gateway.User.GarageId = Guid.NewGuid();
        (await Authenticate(gateway, "admin")).Succeeded.ShouldBeFalse();
    }

    private static Task<IdentityLoginResult> Authenticate(FakeGateway gateway, string identifier) =>
        new IdentityLoginService(gateway).AuthenticateAsync(identifier, "ValidPassword1", default);

    private static FakeGateway ValidGateway()
    {
        var garageId = Guid.NewGuid();
        return new FakeGateway
        {
            User = ApplicationUser.CreateGarageUser("Owner", "owner@test.local", "owner", garageId),
            Roles = [ApplicationRoles.Owner]
        };
    }

    private sealed class FakeGateway : IIdentityLoginGateway
    {
        public ApplicationUser? User { get; set; }
        public SignInResult PasswordResult { get; set; } = SignInResult.Success;
        public IList<string> Roles { get; set; } = [];
        public bool GarageExists { get; set; } = true;
        public int EmailLookups { get; private set; }
        public int NameLookups { get; private set; }
        public int PasswordChecks { get; private set; }

        public Task<ApplicationUser?> FindByEmailAsync(string identifier)
        {
            EmailLookups++;
            return Task.FromResult(User?.Email == identifier ? User : null);
        }

        public Task<ApplicationUser?> FindByNameAsync(string identifier)
        {
            NameLookups++;
            return Task.FromResult(User?.UserName == identifier ? User : null);
        }

        public Task<SignInResult> CheckPasswordAsync(ApplicationUser user, string password)
        {
            PasswordChecks++;
            return Task.FromResult(PasswordResult);
        }

        public Task<IList<string>> GetRolesAsync(ApplicationUser user) => Task.FromResult(Roles);
        public Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user) =>
            Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test")));
        public Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken) => Task.FromResult(GarageExists);
    }
}
