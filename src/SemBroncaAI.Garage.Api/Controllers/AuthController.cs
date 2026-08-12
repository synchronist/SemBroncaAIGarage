using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Infrastructure.Identity;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    IdentityLoginService loginService) : ControllerBase
{
    private const string InvalidCredentialsMessage =
        "Não foi possível entrar com as credenciais informadas.";

    [HttpPost("login")]
    [EnableRateLimiting(AuthenticationRateLimiting.LoginPolicy)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginService.AuthenticateAsync(
            request.Identifier,
            request.Password,
            cancellationToken);
        if (!result.Succeeded)
            return Unauthorized(new AuthErrorResponse(InvalidCredentialsMessage));

        await HttpContext.SignInAsync(
            IdentityConstants.BearerScheme,
            result.Principal!,
            new AuthenticationProperties());

        return new EmptyResult();
    }

    [HttpGet("me")]
    [Authorize(Policy = "ActiveUser")]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new CurrentUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.UserName,
            user.GarageId,
            roles.ToArray()));
    }
}

public sealed record LoginRequest(string Identifier, string Password);
public sealed record AuthErrorResponse(string Message);
public sealed record CurrentUserResponse(
    Guid UserId,
    string Name,
    string? Email,
    string? Username,
    Guid? GarageId,
    IReadOnlyCollection<string> Roles);
