using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/garages")]
public sealed class GaragesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateGarageCommand command,
        [FromServices] CreateGarageHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await handler.HandleAsync(
                command,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IGarageRepository garageRepository,
        CancellationToken cancellationToken)
    {
        var garages = await garageRepository.GetAllAsync(
            cancellationToken);

        return Ok(garages);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetGarageSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var garage = await handler.HandleAsync(id, cancellationToken);

        if (garage is null)
        {
            return NotFound(new
            {
                message = "Oficina não encontrada."
            });
        }

        return Ok(garage);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateGarageSettingsCommand command,
        [FromServices] UpdateGarageSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await handler.HandleAsync(id, command, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
