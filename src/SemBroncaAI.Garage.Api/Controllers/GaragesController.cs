using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerCommand command,
        [FromServices] CreateCustomerHandler handler,
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
        [FromQuery] Guid garageId,
        [FromServices] ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(
            garageId,
            cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (customer is null)
        {
            return NotFound(new
            {
                message = "Cliente não encontrado."
            });
        }

        return Ok(customer);
    }
}