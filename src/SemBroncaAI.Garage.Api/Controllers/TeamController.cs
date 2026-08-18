using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.TeamManagement;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/team")]
[Authorize(Policy = ApplicationPermissions.ManageTeam)]
public sealed class TeamController(ITeamManagement team) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyCollection<TeamMemberItem>> List([FromQuery] string? search, CancellationToken token) => team.ListAsync(search, token);
    [HttpGet("{id:guid}")] public async Task<ActionResult<TeamMemberDetails>> Get(Guid id, CancellationToken token) => await team.GetAsync(id, token) is { } member ? Ok(member) : NotFound();
    [HttpPost("invite")] public async Task<IActionResult> Invite([FromBody] InviteTeamMemberCommand command, CancellationToken token) => Result(await team.InviteAsync(command, token), Created("/team", new { message = "Convite enviado." }));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamMemberCommand command, CancellationToken token) => Result(await team.UpdateAsync(id, command, token), NoContent());
    [HttpPost("{id:guid}/resend-invitation")] public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken token) =>
        Result(await team.ResendInvitationAsync(id, token), Ok(new { message = "Convite reenviado." }));
    private IActionResult Result(TeamOperationResult result, IActionResult success) => result.Succeeded ? success : result.Code switch { "not-found" => NotFound(), "self-protected" or "protected" or "already-active" => Conflict(new { code = result.Code, message = "Esta operação não está disponível para o usuário." }), "conflict" => Conflict(result), _ => BadRequest(result) };
}

[ApiController]
[Route("api/team-invitations")]
[AllowAnonymous]
public sealed class TeamInvitationsController(ITeamManagement team) : ControllerBase
{
    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] Guid id, [FromQuery] string token, CancellationToken cancellationToken) =>
        await team.CanAcceptAsync(id, token, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptTeamInvitationCommand command, CancellationToken token)
    {
        var result = await team.AcceptAsync(command, token);
        return result.Succeeded ? Ok(new { message = "Convite aceito." }) : BadRequest(result);
    }
}
