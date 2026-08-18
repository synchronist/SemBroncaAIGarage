namespace SemBroncaAI.Garage.Application.Features.TeamManagement;

public sealed record TeamMemberItem(Guid Id, string Name, string Email, string UserName, string Role, bool Active);
public sealed record TeamMemberDetails(Guid Id, string Name, string Email, string UserName, string Role, bool Active, bool IsCurrentUser);
public sealed record InviteTeamMemberCommand(string Name, string Email, string UserName, string Role);
public sealed record UpdateTeamMemberCommand(string Name, string Role, bool Active);
public sealed record AcceptTeamInvitationCommand(Guid InvitationId, string Token, string Password, string ConfirmPassword);
public sealed record TeamOperationResult(bool Succeeded, string? Code = null, IReadOnlyDictionary<string, string[]>? Errors = null);

public interface ITeamManagement
{
    Task<IReadOnlyCollection<TeamMemberItem>> ListAsync(string? search, CancellationToken cancellationToken = default);
    Task<TeamMemberDetails?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamOperationResult> InviteAsync(InviteTeamMemberCommand command, CancellationToken cancellationToken = default);
    Task<TeamOperationResult> UpdateAsync(Guid id, UpdateTeamMemberCommand command, CancellationToken cancellationToken = default);
    Task<TeamOperationResult> AcceptAsync(AcceptTeamInvitationCommand command, CancellationToken cancellationToken = default);
}

public interface ITeamInvitationSender
{
    Task SendAsync(string email, string invitationLink, CancellationToken cancellationToken);
}
