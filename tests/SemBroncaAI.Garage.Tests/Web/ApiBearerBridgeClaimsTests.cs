using System.Security.Claims;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class ApiBearerBridgeClaimsTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("Receptionist")]
    public void Financial_roles_should_preserve_official_permissions(string role)
    {
        var garageId = Guid.NewGuid();
        var claims = ApiBearerBridgeClaims.Create(User(role, garageId,
            [ApplicationPermissions.ViewEstimateValues]), "server-token");

        claims.ShouldContain(claim => claim.Type == ApplicationPermissions.ClaimType &&
            claim.Value == ApplicationPermissions.ViewEstimateValues);
        claims.ShouldContain(claim => claim.Type == "garage_id" && claim.Value == garageId.ToString());
    }

    [Fact]
    public void Mechanic_should_not_gain_financial_permission()
    {
        var claims = ApiBearerBridgeClaims.Create(User("Mechanic", Guid.NewGuid(),
            [ApplicationPermissions.ViewServiceOrders]), "server-token");

        claims.ShouldNotContain(claim => claim.Type == ApplicationPermissions.ClaimType &&
            claim.Value == ApplicationPermissions.ViewEstimateValues);
    }

    [Fact]
    public void Platform_admin_should_not_gain_tenant_claims()
    {
        var claims = ApiBearerBridgeClaims.Create(User("PlatformAdmin", null, []), "server-token");
        claims.ShouldNotContain(claim => claim.Type == "garage_id");
        claims.ShouldNotContain(claim => claim.Type == ApplicationPermissions.ClaimType);
    }

    private static CurrentUserModel User(string role, Guid? garageId, IReadOnlyCollection<string> permissions) =>
        new(Guid.NewGuid(), "Test", "test@local", "test", garageId, [role], permissions);
}
