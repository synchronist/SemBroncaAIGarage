using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Infrastructure.Identity;
using Shouldly;
using Microsoft.AspNetCore.Http;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Abstractions.Security;

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
        gateway.PasswordVerifications.ShouldBe(1);
    }

    [Fact]
    public async Task Locked_user_with_wrong_password_should_fail_generically_to_prevent_enumeration()
    {
        var gateway = ValidGateway();
        gateway.PasswordResult = SignInResult.LockedOut;
        gateway.PasswordIsValid = false;

        var result = await Authenticate(gateway, "owner");

        result.ShouldBe(IdentityLoginResult.Failed);
        gateway.PasswordVerifications.ShouldBe(1);
    }

    [Fact]
    public async Task Tenant_user_requires_its_existing_garage()
    {
        var gateway = ValidGateway(); gateway.GarageActive = null;
        var result = await Authenticate(gateway, "owner");
        result.Succeeded.ShouldBeFalse();
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("owner@test.local")]
    public async Task Onboarded_owner_from_inactive_garage_should_receive_specific_block(string identifier)
    {
        var gateway = ValidGateway(); gateway.GarageActive = false;
        var result = await Authenticate(gateway, identifier);
        result.Succeeded.ShouldBeFalse();
        result.IsGarageInactive.ShouldBeTrue();
    }

    [Fact]
    public async Task Inactive_garage_with_wrong_password_should_remain_generic()
    {
        var gateway = ValidGateway();
        gateway.GarageActive = false;
        gateway.PasswordResult = SignInResult.Failed;

        var result = await Authenticate(gateway, "owner");

        result.ShouldBe(IdentityLoginResult.Failed);
        gateway.LastGarageId.ShouldBeNull();
    }

    [Fact]
    public async Task Inactive_garage_should_leave_api_with_stable_code_and_safe_message()
    {
        var gateway = ValidGateway(); gateway.GarageActive = false;
        var controller = new AuthController(null!, new IdentityLoginService(gateway))
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginRequest("owner", "ValidPassword1"), default);

        var forbidden = result.ShouldBeOfType<Microsoft.AspNetCore.Mvc.ObjectResult>();
        forbidden.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        var error = forbidden.Value.ShouldBeOfType<AuthErrorResponse>();
        error.Code.ShouldBe(AuthenticationErrorCodes.GarageInactive);
        error.Message.ShouldBe("O acesso desta oficina está temporariamente indisponível.");
    }

    [Fact]
    public async Task Missing_user_should_leave_api_without_specific_state_code()
    {
        var gateway = ValidGateway(); gateway.User = null;
        var controller = new AuthController(null!, new IdentityLoginService(gateway))
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginRequest("missing", "ValidPassword1"), default);

        var error = result.ShouldBeOfType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>().Value.ShouldBeOfType<AuthErrorResponse>();
        error.Code.ShouldBeNull();
        error.Message.ShouldBe("Não foi possível entrar com as credenciais informadas.");
    }

    [Fact]
    public async Task Garage_status_should_be_revalidated_for_the_users_current_tenant()
    {
        var gateway = ValidGateway();
        var garageId = gateway.User!.GarageId;

        (await Authenticate(gateway, "owner")).Succeeded.ShouldBeTrue();

        gateway.LastGarageId.ShouldBe(garageId);
        gateway.GarageActive = false;
        (await Authenticate(gateway, "owner")).IsGarageInactive.ShouldBeTrue();
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
        public bool? GarageActive { get; set; } = true;
        public int EmailLookups { get; private set; }
        public int NameLookups { get; private set; }
        public int PasswordChecks { get; private set; }
        public bool PasswordIsValid { get; set; } = true;
        public int PasswordVerifications { get; private set; }
        public Guid? LastGarageId { get; private set; }

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

        public Task<bool> VerifyPasswordAsync(ApplicationUser user, string password)
        {
            PasswordVerifications++;
            return Task.FromResult(PasswordIsValid);
        }

        public Task<IList<string>> GetRolesAsync(ApplicationUser user) => Task.FromResult(Roles);
        public Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user) =>
            Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test")));
        public Task<bool?> GetGarageActiveAsync(Guid garageId, CancellationToken cancellationToken)
        {
            LastGarageId = garageId;
            return Task.FromResult(GarageActive);
        }
    }
}
