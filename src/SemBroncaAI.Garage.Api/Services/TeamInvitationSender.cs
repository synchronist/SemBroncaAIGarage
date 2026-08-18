using SemBroncaAI.Garage.Application.Features.TeamManagement;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class DevelopmentTeamInvitationSender(ILogger<DevelopmentTeamInvitationSender> logger) : ITeamInvitationSender
{
    public Task SendAsync(string email, string invitationLink, CancellationToken cancellationToken)
    {
        logger.LogWarning("DEVELOPMENT ONLY - team invitation for {Email}: {InvitationLink}", email, invitationLink);
        return Task.CompletedTask;
    }
}

public sealed class UnavailableTeamInvitationSender : ITeamInvitationSender
{
    public Task SendAsync(string email, string invitationLink, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("O envio de convites não está configurado.");
}
