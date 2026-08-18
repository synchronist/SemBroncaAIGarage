using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Application.Abstractions.Email;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "Development";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "SemBroncaAI Garage";
    public bool UseSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 15;
}

public sealed class SmtpTransactionalEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    public async Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default)
    {
        var configuration = options.Value;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(configuration.FromAddress!, configuration.FromName, System.Text.Encoding.UTF8),
                Subject = message.Subject,
                SubjectEncoding = System.Text.Encoding.UTF8,
                Body = message.HtmlBody,
                BodyEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(message.Recipient));
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                message.TextBody, System.Text.Encoding.UTF8, "text/plain"));
            using var smtp = new SmtpClient(configuration.Host!, configuration.Port)
            {
                EnableSsl = configuration.UseSsl,
                Timeout = checked(configuration.TimeoutSeconds * 1000),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(configuration.Username, configuration.Password)
            };
            await smtp.SendMailAsync(mail, cancellationToken);
            logger.LogInformation("Transactional email {EmailType} sent to {Recipient} in {ElapsedMs} ms",
                message.Type, Mask(message.Recipient), stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception) when (exception is SmtpException or FormatException or InvalidOperationException)
        {
            logger.LogError(exception, "Transactional email {EmailType} failed for {Recipient} after {ElapsedMs} ms",
                message.Type, Mask(message.Recipient), stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static string Mask(string email)
    {
        var separator = email.IndexOf('@');
        return separator <= 1 ? "***" : $"{email[0]}***{email[(separator - 1)..]}";
    }
}

public sealed class DevelopmentTransactionalEmailSender(
    ILogger<DevelopmentTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    public Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("DEVELOPMENT ONLY - transactional email {EmailType} for {Recipient}: {Link}",
            message.Type, message.Recipient, message.DevelopmentLink ?? "sem link");
        return Task.CompletedTask;
    }
}
