using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;

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
        [FromServices] IGarageRepository garageRepository,
        CancellationToken cancellationToken)
    {
        var garage = await garageRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (garage is null)
        {
            return NotFound(new
            {
                message = "Oficina não encontrada."
            });
        }

        return Ok(garage);
    }
}