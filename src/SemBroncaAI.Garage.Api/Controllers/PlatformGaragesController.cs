using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/platform/garages")]
[Authorize(Policy = PlatformAuthorization.Policy)]
public sealed class PlatformGaragesController(IPlatformGarageAdministration administration) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<PlatformDashboardResponse> Dashboard(CancellationToken cancellationToken) =>
        administration.GetDashboardAsync(cancellationToken);

    [HttpGet]
    public async Task<ActionResult<PlatformGarageListResponse>> List(
        [FromQuery] string? search, [FromQuery] bool? active, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await administration.ListAsync(new(search, active, page, pageSize), cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlatformGarageDetailsResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var garage = await administration.GetByIdAsync(id, cancellationToken);
        return garage is null ? NotFound() : Ok(garage);
    }

    [HttpPost]
    public async Task<ActionResult<CreatePlatformGarageResponse>> Create(
        [FromBody] CreatePlatformGarageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var created = await administration.CreateAsync(command, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.GarageId }, created);
        }
        catch (PlatformGarageValidationException exception)
        {
            return BadRequest(new PlatformGarageValidationErrorResponse(
                "Revise os campos destacados abaixo.", exception.Errors));
        }
        catch (PlatformGarageConflictException exception)
        {
            return Conflict(new PlatformGarageValidationErrorResponse(
                "Não foi possível concluir o cadastro.",
                new Dictionary<string, string[]> { [exception.Field] = [exception.Message] }));
        }
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetGarageActiveRequest request,
        CancellationToken cancellationToken) =>
        await administration.SetActiveAsync(id, request.Active, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("{id:guid}/subscription")]
    public async Task<ActionResult<PlatformSubscriptionResponse>> UpdateSubscription(
        Guid id, [FromBody] UpdateGarageSubscriptionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await administration.UpdateSubscriptionAsync(id, command, cancellationToken);
            return subscription is null ? NotFound() : Ok(subscription);
        }
        catch (PlatformGarageValidationException exception)
        {
            return BadRequest(new PlatformGarageValidationErrorResponse("Revise os dados da assinatura.", exception.Errors));
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new PlatformGarageValidationErrorResponse("Revise os dados da assinatura.",
                new Dictionary<string, string[]> { ["trialEndsAt"] = ["O fim do trial deve estar no futuro."] }));
        }
    }

}

public sealed record SetGarageActiveRequest(bool Active);
public sealed record PlatformGarageValidationErrorResponse(
    string Message,
    IReadOnlyDictionary<string, string[]> Errors);
