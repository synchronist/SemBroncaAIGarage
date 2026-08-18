namespace SemBroncaAI.Garage.Application.Abstractions.Email;

public sealed record TransactionalEmailMessage(
    string Type,
    string Recipient,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? DevelopmentLink = null);

public interface ITransactionalEmailSender
{
    Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default);
}
