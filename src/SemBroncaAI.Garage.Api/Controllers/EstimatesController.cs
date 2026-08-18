using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/estimates")]
[Authorize(Policy = "TenantUser")]
public sealed class EstimatesController(ICurrentGarage currentGarage) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.ViewEstimateValues)]
    public async Task<IActionResult> List(
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
                new ListEstimatesQuery(currentGarage.RequireGarageId(), search, status, page, pageSize), cancellationToken));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
