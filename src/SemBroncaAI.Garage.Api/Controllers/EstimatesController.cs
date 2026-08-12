using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/estimates")]
public sealed class EstimatesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid garageId,
        [FromQuery] string? search,
        [FromQuery] EstimateCommercialStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] ListEstimatesHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await handler.HandleAsync(
                new ListEstimatesQuery(garageId, search, status, page, pageSize), cancellationToken));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
