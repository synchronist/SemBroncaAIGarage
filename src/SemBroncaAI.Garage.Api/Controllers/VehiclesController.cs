using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateVehicleCommand command,
        [FromServices] CreateVehicleHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(Create),
            new { id = response.Id },
            response);
    }
}