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
    private IActionResult Result(TeamOperationResult result, IActionResult success) => result.Succeeded ? success : result.Code switch { "not-found" => NotFound(), "self-protected" or "protected" => Conflict(new { code = result.Code, message = "O proprietário principal não pode ser alterado." }), "conflict" => Conflict(result), _ => BadRequest(result) };
}

[ApiController]
[Route("api/team-invitations")]
[AllowAnonymous]
public sealed class TeamInvitationsController(ITeamManagement team) : ControllerBase
{
    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptTeamInvitationCommand command, CancellationToken token)
    {
        var result = await team.AcceptAsync(command, token);
        return result.Succeeded ? Ok(new { message = "Convite aceito." }) : BadRequest(result);
    }
}
