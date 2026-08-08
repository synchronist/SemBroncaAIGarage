using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;
using SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;
using SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;
using SemBroncaAI.Garage.Application.Features.Customers.UpdateCustomer;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersModuleController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid garageId, [FromQuery] string? search,
        [FromQuery] int page, [FromQuery] int pageSize,
        [FromServices] ListCustomersHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(new ListCustomersQuery(garageId, search, page, pageSize), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id, [FromQuery] Guid garageId,
        [FromServices] GetCustomerByIdHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, garageId, cancellationToken);
        return response is null ? NotFound(new { message = "Cliente não encontrado." }) : Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerCommand command,
        [FromServices] CreateCustomerHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            var response = await handler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id, garageId = response.GarageId }, response);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCustomerCommand command,
        [FromServices] UpdateCustomerHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await handler.HandleAsync(id, command, cancellationToken));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
