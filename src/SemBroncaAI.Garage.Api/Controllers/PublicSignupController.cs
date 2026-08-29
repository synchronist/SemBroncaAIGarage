using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Api.Services;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/public/signup")]
[AllowAnonymous]
[EnableRateLimiting(PublicSignupRateLimiting.PolicyName)]
public sealed class PublicSignupController(
    IPublicGarageSignup signup,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreatePlatformGarageResponse>> Create(
        [FromBody] PublicGarageSignupCommand command, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("PublicSignup:Enabled"))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "O cadastro público ainda não está disponível." });
        try
        {
            return Ok(await signup.SignupAsync(command, cancellationToken));
        }
        catch (PlatformGarageValidationException exception)
        {
            return BadRequest(new PlatformGarageValidationErrorResponse(
                "Revise os campos destacados abaixo.", exception.Errors));
        }
        catch (PlatformGarageConflictException)
        {
            return Conflict(new { message = "Não foi possível concluir o cadastro com os dados informados." });
        }
    }
}
