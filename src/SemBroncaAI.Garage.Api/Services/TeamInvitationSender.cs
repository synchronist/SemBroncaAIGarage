using System.Net;
using SemBroncaAI.Garage.Application.Abstractions.Email;
using SemBroncaAI.Garage.Application.Features.TeamManagement;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class TeamInvitationEmailSender(ITransactionalEmailSender sender) : ITeamInvitationSender
{
    public Task SendAsync(TeamInvitationEmail invitation, CancellationToken cancellationToken)
    {
        var garage = WebUtility.HtmlEncode(invitation.GarageName);
        var link = WebUtility.HtmlEncode(invitation.InvitationLink);
        var role = invitation.Role == "Mechanic" ? "Mecânico" : "Recepção";
        var expiration = invitation.ExpiresAt.ToString("dd/MM/yyyy 'às' HH:mm 'UTC'");
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;color:#1f2937">
              <h1 style="color:#ea580c">SemBroncaAI Garage</h1>
              <p>Você foi convidado para acessar a oficina <strong>{garage}</strong> como <strong>{role}</strong>.</p>
              <p><a href="{link}" style="display:inline-block;padding:12px 18px;background:#ea580c;color:white;text-decoration:none;border-radius:8px">Aceitar convite</a></p>
              <p>Este convite é válido até {expiration}.</p><p>Se você não reconhece este convite, ignore esta mensagem.</p>
              <p>Alternativa: <a href="{link}">{link}</a></p>
            </div>
            """;
        var text = $"SemBroncaAI Garage\n\nVocê foi convidado para a oficina {invitation.GarageName} como {role}.\nAceitar convite: {invitation.InvitationLink}\nVálido até {expiration}.\nSe não reconhece este convite, ignore esta mensagem.";
        return sender.SendAsync(new("team-invitation", invitation.Email, "Convite para o SemBroncaAI Garage",
            html, text, invitation.InvitationLink), cancellationToken);
    }
}
