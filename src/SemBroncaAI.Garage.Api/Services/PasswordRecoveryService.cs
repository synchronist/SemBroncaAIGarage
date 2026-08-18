using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Application.Abstractions.Email;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace SemBroncaAI.Garage.Api.Services;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string email, string resetLink, CancellationToken cancellationToken);
}

public sealed class PasswordResetEmailSender(ITransactionalEmailSender sender) : IPasswordResetEmailSender
{
    public Task SendAsync(string email, string resetLink, CancellationToken cancellationToken)
    {
        var link = WebUtility.HtmlEncode(resetLink);
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;color:#1f2937">
              <h1 style="color:#ea580c">SemBroncaAI Garage</h1><h2>Redefinição de senha</h2>
              <p>Recebemos uma solicitação para redefinir sua senha.</p>
              <p><a href="{link}" style="display:inline-block;padding:12px 18px;background:#ea580c;color:white;text-decoration:none;border-radius:8px">Redefinir senha</a></p>
              <p>O link expira em duas horas. Se você não fez esta solicitação, ignore esta mensagem.</p>
              <p>Alternativa: <a href="{link}">{link}</a></p>
            </div>
            """;
        var text = $"SemBroncaAI Garage\n\nRedefinição de senha\n{resetLink}\nO link expira em duas horas. Se não solicitou, ignore esta mensagem.";
        return sender.SendAsync(new("password-reset", email, "Redefinição de senha — SemBroncaAI Garage",
            html, text, resetLink), cancellationToken);
    }
}

public interface IPasswordRecoveryGateway
{
    Task<ApplicationUser?> FindByEmailAsync(string email);
    Task<ApplicationUser?> FindByIdAsync(Guid userId);
    Task<string> GenerateTokenAsync(ApplicationUser user);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<bool> VerifyTokenAsync(ApplicationUser user, string token);
    Task<IdentityResult> ResetAsync(ApplicationUser user, string token, string password);
    Task UpdateSecurityStampAsync(ApplicationUser user);
    Task<bool> IsGarageEligibleAsync(ApplicationUser user, CancellationToken cancellationToken);
}

public sealed class IdentityPasswordRecoveryGateway(UserManager<ApplicationUser> userManager,
    SemBroncaAI.Garage.Infrastructure.Persistence.GarageDbContext context) : IPasswordRecoveryGateway
{
    public Task<ApplicationUser?> FindByEmailAsync(string email) => userManager.FindByEmailAsync(email);
    public Task<ApplicationUser?> FindByIdAsync(Guid userId) => userManager.FindByIdAsync(userId.ToString());
    public Task<string> GenerateTokenAsync(ApplicationUser user) => userManager.GeneratePasswordResetTokenAsync(user);
    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) => userManager.CheckPasswordAsync(user, password);
    public Task<bool> VerifyTokenAsync(ApplicationUser user, string token) => userManager.VerifyUserTokenAsync(
        user, userManager.Options.Tokens.PasswordResetTokenProvider, UserManager<ApplicationUser>.ResetPasswordTokenPurpose, token);
    public Task<IdentityResult> ResetAsync(ApplicationUser user, string token, string password) =>
        userManager.ResetPasswordAsync(user, token, password);
    public Task UpdateSecurityStampAsync(ApplicationUser user) => userManager.UpdateSecurityStampAsync(user);
    public Task<bool> IsGarageEligibleAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        user.GarageId is null
            ? Task.FromResult(true)
            : context.Garages.AsNoTracking().AnyAsync(x => x.Id == user.GarageId && x.Active, cancellationToken);
}

public sealed class PasswordRecoveryService(
    IPasswordRecoveryGateway gateway,
    IPasswordResetEmailSender emailSender,
    IConfiguration configuration,
    ILogger<PasswordRecoveryService> logger)
{
    public async Task RequestAsync(string? email, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("PasswordRecovery:Enabled", false)) return;
        if (string.IsNullOrWhiteSpace(email)) return;
        var user = await gateway.FindByEmailAsync(email.Trim());
        if (user is null || !user.Active || !user.EmailConfirmed || !await gateway.IsGarageEligibleAsync(user, cancellationToken)) return;

        var token = await gateway.GenerateTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var baseUrl = configuration["App:PublicBaseUrl"]
            ?? throw new InvalidOperationException("Configure App:PublicBaseUrl para recuperação de senha.");
        var link = $"{baseUrl.TrimEnd('/')}/reset-password?userId={user.Id:D}&token={Uri.EscapeDataString(encodedToken)}";
        try
        {
            await emailSender.SendAsync(user.Email!, link, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Password reset email delivery failed");
        }
    }

    public async Task<PasswordResetResult> ResetAsync(
        Guid userId, string? encodedToken, string? password, string? confirmation)
    {
        if (string.IsNullOrWhiteSpace(encodedToken) || string.IsNullOrEmpty(password))
            return PasswordResetResult.Invalid;
        if (password != confirmation) return PasswordResetResult.Mismatch;
        var user = await gateway.FindByIdAsync(userId);
        if (user is null || !user.Active || !user.EmailConfirmed) return PasswordResetResult.Invalid;

        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken)); }
        catch (FormatException) { return PasswordResetResult.Invalid; }

        if (!await gateway.VerifyTokenAsync(user, token)) return PasswordResetResult.Invalid;

        if (await gateway.CheckPasswordAsync(user, password))
            return PasswordResetResult.SamePassword;

        var result = await gateway.ResetAsync(user, token, password);
        if (!result.Succeeded)
        {
            var messages = PasswordPolicyMessages.From(result.Errors);
            return messages.Count > 0 ? PasswordResetResult.Rejected(messages) : PasswordResetResult.Invalid;
        }

        await gateway.UpdateSecurityStampAsync(user);
        return PasswordResetResult.Success;
    }
}

public enum PasswordResetStatus { Success, Invalid, PasswordRejected, Mismatch, SamePassword }

public sealed record PasswordResetResult(PasswordResetStatus Status, IReadOnlyCollection<string> Messages)
{
    public static PasswordResetResult Success { get; } = new(PasswordResetStatus.Success, []);
    public static PasswordResetResult Invalid { get; } = new(PasswordResetStatus.Invalid, []);
    public static PasswordResetResult Mismatch { get; } = new(PasswordResetStatus.Mismatch, ["As senhas não coincidem."]);
    public static PasswordResetResult SamePassword { get; } = new(PasswordResetStatus.SamePassword, ["A nova senha deve ser diferente da senha atual."]);
    public static PasswordResetResult Rejected(IReadOnlyCollection<string> messages) => new(PasswordResetStatus.PasswordRejected, messages);
}

public static class PasswordPolicyMessages
{
    public static IReadOnlyCollection<string> From(IEnumerable<IdentityError> errors) => errors
        .Select(error => error.Code switch
        {
            "PasswordTooShort" => "A senha deve ter pelo menos 10 caracteres.",
            "PasswordRequiresUpper" => "Adicione pelo menos uma letra maiúscula.",
            "PasswordRequiresLower" => "Adicione pelo menos uma letra minúscula.",
            "PasswordRequiresDigit" => "Adicione pelo menos um número.",
            "PasswordRequiresUniqueChars" => "Use pelo menos quatro caracteres diferentes.",
            _ => null
        })
        .Where(message => message is not null)
        .Cast<string>()
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}
