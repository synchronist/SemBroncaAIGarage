using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

    [Fact]
    public void Resend_should_rotate_token_and_track_delivery_without_reviving_used_invitation()
    {
        var invitation = new TeamInvitationEntity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), DateTime.UtcNow.AddMinutes(5));
        invitation.MarkDeliveryFailed(DateTime.UtcNow);
        invitation.DeliveryStatus.ShouldBe(InvitationDeliveryStatus.Failed);
        invitation.Renew(new string('B', 64), DateTime.UtcNow.AddHours(24));
        invitation.TokenHash.ShouldBe(new string('B', 64));
        invitation.DeliveryStatus.ShouldBe(InvitationDeliveryStatus.Created);
        invitation.MarkSent(DateTime.UtcNow);
        invitation.DeliveryStatus.ShouldBe(InvitationDeliveryStatus.Sent);
        invitation.MarkUsed(DateTime.UtcNow);
        Should.Throw<InvalidOperationException>(() => invitation.Renew(new string('C', 64), DateTime.UtcNow.AddHours(24)));
    }

    [Fact]
    public void Consecutive_resends_should_leave_only_the_last_token_usable()
    {
        var now = DateTime.UtcNow;
        const string tokenA = "token-A";
        const string tokenB = "token-B";
        const string tokenC = "token-C";
        var invitation = new TeamInvitationEntity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Hash(tokenA), now.AddHours(24));

        invitation.Renew(Hash(tokenB), now.AddHours(24));
        invitation.MatchesTokenHash(Hash(tokenA), now).ShouldBeFalse();
        invitation.MatchesTokenHash(Hash(tokenB), now).ShouldBeTrue();
        invitation.Renew(Hash(tokenC), now.AddHours(24));

        invitation.MatchesTokenHash(Hash(tokenA), now).ShouldBeFalse();
        invitation.MatchesTokenHash(Hash(tokenB), now).ShouldBeFalse();
        invitation.MatchesTokenHash(Hash(tokenC), now).ShouldBeTrue();
        invitation.MarkUsed(now);
        invitation.MatchesTokenHash(Hash(tokenC), now).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => invitation.Renew(new string('D', 64), now.AddHours(24)));
    }

    [Fact]
    public void Resend_should_invalidate_other_pending_historical_invitations()
    {
        var now = DateTime.UtcNow;
        var previous = new TeamInvitationEntity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), now.AddHours(24));

        previous.Invalidate(now);

        previous.CanUse(now).ShouldBeFalse();
    }

    [Fact]
    public void Team_invitation_pages_should_expose_transient_success_and_failure_feedback()
    {
        var pages = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SemBroncaAI.Garage.Web", "Components", "Pages");
        var invitePage = File.ReadAllText(Path.Combine(pages, "TeamNew.razor"));
        var detailsPage = File.ReadAllText(Path.Combine(pages, "TeamDetails.razor"));

        invitePage.ShouldContain("Snackbar.Add(\"Convite criado com sucesso.\",Severity.Success)");
        invitePage.ShouldContain("Não foi possível enviar o convite. Tente novamente.");
        invitePage.ShouldNotContain("NavigateTo(\"/team?");
        detailsPage.ShouldContain("Snackbar.Add(_success,Severity.Success)");
        detailsPage.ShouldContain("Não foi possível reenviar o convite. Tente novamente.");
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

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
