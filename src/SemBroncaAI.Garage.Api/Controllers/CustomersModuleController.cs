using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;
using SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;
using SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;
using SemBroncaAI.Garage.Application.Features.Customers.UpdateCustomer;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = "TenantUser")]
public sealed class CustomersModuleController(ICurrentGarage currentGarage) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page, [FromQuery] int pageSize,
        [FromServices] ListCustomersHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(new ListCustomersQuery(currentGarage.RequireGarageId(), search, page, pageSize), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetCustomerByIdHandler handler, CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, currentGarage.RequireGarageId(), cancellationToken);
        return response is null ? NotFound(new { message = "Cliente não encontrado." }) : Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveCustomerRequest request,
        [FromServices] CreateCustomerHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateCustomerCommand(currentGarage.RequireGarageId(), request.Name, request.Document, request.Phone, request.Email);
            var response = await handler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] SaveCustomerRequest request,
        [FromServices] UpdateCustomerHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateCustomerCommand(currentGarage.RequireGarageId(), request.Name, request.Document, request.Phone, request.Email);
            return Ok(await handler.HandleAsync(id, command, cancellationToken));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}

public sealed record SaveCustomerRequest(string Name, string Document, string Phone, string Email);
