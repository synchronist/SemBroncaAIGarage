using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Api.Services;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/auth/password")]
[AllowAnonymous]
public sealed class PasswordRecoveryController(PasswordRecoveryService service) : ControllerBase
{
    public const string NeutralMessage =
        "Se existir uma conta associada a este e-mail, enviaremos as instruções para redefinição da senha.";

    [HttpPost("forgot")]
    [EnableRateLimiting(AuthenticationRateLimiting.PasswordRecoveryPolicy)]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await service.RequestAsync(request.Email, cancellationToken);
        return Ok(new { message = NeutralMessage });
    }

    [HttpPost("reset")]
    [EnableRateLimiting(AuthenticationRateLimiting.PasswordRecoveryPolicy)]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest request)
    {
        var result = await service.ResetAsync(request.UserId, request.Token, request.Password, request.ConfirmPassword);
        return result.Status switch
        {
            PasswordResetStatus.Success => Ok(new { message = "Senha redefinida com sucesso." }),
            PasswordResetStatus.Mismatch => BadRequest(new { code = "mismatch", messages = result.Messages }),
            PasswordResetStatus.SamePassword => BadRequest(new { code = "same-password", messages = result.Messages }),
            PasswordResetStatus.PasswordRejected => BadRequest(new { code = "password", messages = result.Messages }),
            _ => BadRequest(new { code = "invalid", messages = new[] { "Este link de recuperação é inválido ou expirou." } })
        };
    }
}

public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(Guid UserId, string Token, string Password, string ConfirmPassword);
