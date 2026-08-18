using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.TeamManagement;
using SemBroncaAI.Garage.Domain.Entities;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class TeamManagementTests
{
    [Fact]
    public void Team_api_should_require_manage_team_while_invitation_acceptance_is_public()
    {
        typeof(TeamController).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(ApplicationPermissions.ManageTeam);
        typeof(TeamInvitationsController).GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Owner]).ShouldContain(ApplicationPermissions.ManageTeam);
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Receptionist]).ShouldNotContain(ApplicationPermissions.ManageTeam);
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Mechanic]).ShouldNotContain(ApplicationPermissions.ManageTeam);
        RolePermissionDefaults.ForRoles(["PlatformAdmin"]).ShouldNotContain(ApplicationPermissions.ManageTeam);
    }

    [Fact]
    public void Invitation_should_expire_and_be_single_use()
    {
        var invitation = new TeamInvitationEntity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), DateTime.UtcNow.AddMinutes(5));
        invitation.CanUse(DateTime.UtcNow).ShouldBeTrue();
        invitation.MarkUsed(DateTime.UtcNow);
        invitation.CanUse(DateTime.UtcNow).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => invitation.MarkUsed(DateTime.UtcNow));

        var expired = new TeamInvitationEntity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('B', 64), DateTime.UtcNow.AddSeconds(-1));
        expired.CanUse(DateTime.UtcNow).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Receptionist")]
    [InlineData("Mechanic")]
    public void First_version_should_expose_only_supported_member_roles(string role)
    {
        var command = new InviteTeamMemberCommand("Membro", "member@test.local", "member", role);
        command.Role.ShouldBeOneOf("Receptionist", "Mechanic");
    }

    [Fact]
    public void Team_contracts_should_not_accept_garage_or_role_during_invitation_acceptance()
    {
        typeof(InviteTeamMemberCommand).GetProperties().Select(x => x.Name).ShouldNotContain("GarageId");
        typeof(AcceptTeamInvitationCommand).GetProperties().Select(x => x.Name)
            .ShouldBe(["InvitationId", "Token", "Password", "ConfirmPassword"]);
    }
}
