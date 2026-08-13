using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Lookup;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/lookup")]
[Authorize(Policy = "TenantUser")]
public sealed class LookupController(ICurrentGarage currentGarage) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromServices] SearchLookupHandler handler,
        CancellationToken cancellationToken)
    {
        var results = await handler.HandleAsync(
            currentGarage.RequireGarageId(),
            query,
            cancellationToken);

        return Ok(results);
    }
}
