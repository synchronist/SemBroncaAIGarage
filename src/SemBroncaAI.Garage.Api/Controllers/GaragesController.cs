using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UploadGarageLogo;
using SemBroncaAI.Garage.Application.Abstractions.Storage;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/garage")]
[Authorize(Policy = "TenantUser")]
public sealed class GaragesController(ICurrentGarage currentGarage) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(
        [FromServices] GetGarageSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var garage = await handler.HandleAsync(currentGarage.RequireGarageId(), cancellationToken);

        if (garage is null)
        {
            return NotFound(new
            {
                message = "Oficina não encontrada."
            });
        }

        return Ok(garage);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateGarageSettingsCommand command,
        [FromServices] UpdateGarageSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await handler.HandleAsync(currentGarage.RequireGarageId(), command, cancellationToken));
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

    [HttpPost("logo")]
    [RequestSizeLimit(UploadGarageLogoHandler.MaximumBytes + 64 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file,
        [FromServices] UploadGarageLogoHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await handler.HandleAsync(currentGarage.RequireGarageId(), stream, file.Length, file.ContentType, cancellationToken));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo([FromServices] IGarageRepository repository,
        [FromServices] IBrandAssetStorage storage, CancellationToken cancellationToken)
    {
        var garage = await repository.GetByIdAsync(currentGarage.RequireGarageId(), cancellationToken);
        if (garage?.LogoStorageKey is null) return NotFound();
        var asset = await storage.OpenAsync(garage.LogoStorageKey, cancellationToken);
        return asset is null ? NotFound() : File(asset.Content, asset.ContentType);
    }
}
