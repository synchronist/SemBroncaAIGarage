using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Infrastructure.Identity;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class CurrentTenantTests
{
    [Fact]
    public void Authenticated_tenant_should_resolve_user_garage_and_roles_from_claims()
    {
        var userId = Guid.NewGuid(); var garageId = Guid.NewGuid();
        var current = CreateCurrentUser(
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("garage_id", garageId.ToString()),
            new(ClaimTypes.Role, ApplicationRoles.Owner));

        current.IsAuthenticated.ShouldBeTrue();
        current.UserId.ShouldBe(userId);
        current.GarageId.ShouldBe(garageId);
        current.RequireGarageId().ShouldBe(garageId);
        current.Roles.ShouldContain(ApplicationRoles.Owner);
    }

    [Fact]
    public void Anonymous_user_should_not_obtain_tenant_context()
    {
        var current = new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        current.IsAuthenticated.ShouldBeFalse();
        Should.Throw<UnauthorizedAccessException>(() => current.RequireGarageId());
    }

    [Fact]
    public void Platform_admin_without_garage_should_not_enter_tenant_context()
    {
        var current = CreateCurrentUser(
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, ApplicationRoles.PlatformAdmin));
        current.IsPlatformAdmin.ShouldBeTrue();
        Should.Throw<UnauthorizedAccessException>(() => current.RequireGarageId());
    }

    [Fact]
    public void Migrated_api_contracts_should_not_accept_external_garage_id()
    {
        var controllerTypes = new[]
        {
            typeof(CustomersModuleController), typeof(VehiclesController), typeof(LookupController),
            typeof(EstimatesController), typeof(ServiceOrdersController), typeof(GaragesController)
        };

        foreach (var parameter in controllerTypes.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(method => method.GetParameters()))
        {
            parameter.Name.ShouldNotBe("garageId");
            parameter.ParameterType.GetProperties().Select(property => property.Name)
                .ShouldNotContain("GarageId");
        }
    }

    [Fact]
    public void Only_auth_login_and_public_approval_are_explicitly_anonymous()
    {
        typeof(PublicApprovalsController).GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
        typeof(AuthController).GetMethod(nameof(AuthController.Login))!
            .GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
        typeof(AuthController).GetMethod(nameof(AuthController.Me))!
            .GetCustomAttribute<AllowAnonymousAttribute>().ShouldBeNull();
    }

    [Fact]
    public void Current_user_contract_should_not_depend_on_aspnet_core()
    {
        typeof(ICurrentUser).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name)
            .ShouldNotContain("Microsoft.AspNetCore.Http.Abstractions");
    }

    private static HttpContextCurrentUser CreateCurrentUser(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        return new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = context });
    }
}
