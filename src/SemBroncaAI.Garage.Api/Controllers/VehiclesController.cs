using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;
using SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
using SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
using SemBroncaAI.Garage.Application.Features.Vehicles.UpdateVehicle;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;
namespace SemBroncaAI.Garage.Api.Controllers;
[ApiController, Route("api/vehicles")]
[Authorize(Policy = "TenantUser")]
public sealed class VehiclesController(ICurrentGarage currentGarage) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.ViewCustomersVehicles)]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page, [FromQuery] int pageSize, [FromServices] ListVehiclesHandler handler, CancellationToken cancellationToken)
    {
        try { return Ok(await handler.HandleAsync(new ListVehiclesQuery(currentGarage.RequireGarageId(), search, page, pageSize), cancellationToken)); }
        catch (ArgumentOutOfRangeException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = ApplicationPermissions.ViewCustomersVehicles)]
    public async Task<IActionResult> GetById(Guid id, [FromServices] GetVehicleByIdHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, currentGarage.RequireGarageId(), cancellationToken);
        return response is null ? NotFound(new { message = "Veículo não encontrado." }) : Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPermissions.ManageCustomersVehicles)]
    public Task<IActionResult> Create([FromBody] SaveVehicleRequest request, [FromServices] CreateVehicleHandler handler, CancellationToken cancellationToken) =>
        Execute(async () => { var command = request.ToCreate(currentGarage.RequireGarageId()); var response = await handler.HandleAsync(command, cancellationToken); return CreatedAtAction(nameof(GetById), new { id = response.Id }, response); });

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ApplicationPermissions.ManageCustomersVehicles)]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveVehicleRequest request, [FromServices] UpdateVehicleHandler handler, CancellationToken cancellationToken) =>
        Execute(async () => Ok(await handler.HandleAsync(id, request.ToUpdate(currentGarage.RequireGarageId()), cancellationToken)));

    private static async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return new BadRequestObjectResult(new { message = exception.Message }); }
    }
}
public sealed record SaveVehicleRequest(Guid CustomerId, string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel, int Mileage)
{
    public CreateVehicleCommand ToCreate(Guid garageId) => new(garageId, CustomerId, Plate, Brand, Model, Version, Year, Color, Fuel, Mileage);
    public UpdateVehicleCommand ToUpdate(Guid garageId) => new(garageId, CustomerId, Plate, Brand, Model, Version, Year, Color, Fuel, Mileage);
}
