using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;
using SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
using SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
using SemBroncaAI.Garage.Application.Features.Vehicles.UpdateVehicle;
namespace SemBroncaAI.Garage.Api.Controllers;
[ApiController, Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid garageId, [FromQuery] string? search, [FromQuery] int page, [FromQuery] int pageSize, [FromServices] ListVehiclesHandler handler, CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new ListVehiclesQuery(garageId, search, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid garageId, [FromServices] GetVehicleByIdHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, garageId, cancellationToken);
        return response is null ? NotFound(new { message = "Veículo não encontrado." }) : Ok(response);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateVehicleCommand command, [FromServices] CreateVehicleHandler handler, CancellationToken cancellationToken) =>
        Execute(async () => { var response = await handler.HandleAsync(command, cancellationToken); return CreatedAtAction(nameof(GetById), new { id = response.Id, garageId = response.GarageId }, response); });

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleCommand command, [FromServices] UpdateVehicleHandler handler, CancellationToken cancellationToken) =>
        Execute(async () => Ok(await handler.HandleAsync(id, command, cancellationToken)));

    private static async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return new BadRequestObjectResult(new { message = exception.Message }); }
    }
}
