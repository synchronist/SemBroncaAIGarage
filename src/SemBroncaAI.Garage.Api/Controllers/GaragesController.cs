using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UploadGarageLogo;
using SemBroncaAI.Garage.Application.Abstractions.Storage;

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

    [HttpPost("{id:guid}/logo")]
    [RequestSizeLimit(UploadGarageLogoHandler.MaximumBytes + 64 * 1024)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file,
        [FromServices] UploadGarageLogoHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await handler.HandleAsync(id, stream, file.Length, file.ContentType, cancellationToken));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/logo")]
    public async Task<IActionResult> GetLogo(Guid id, [FromServices] IGarageRepository repository,
        [FromServices] IBrandAssetStorage storage, CancellationToken cancellationToken)
    {
        var garage = await repository.GetByIdAsync(id, cancellationToken);
        if (garage?.LogoStorageKey is null) return NotFound();
        var asset = await storage.OpenAsync(garage.LogoStorageKey, cancellationToken);
        return asset is null ? NotFound() : File(asset.Content, asset.ContentType);
    }
}
