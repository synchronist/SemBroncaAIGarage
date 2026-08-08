using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Lookup;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/lookup")]
public sealed class LookupController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid garageId,
        [FromQuery] string query,
        [FromServices] SearchLookupHandler handler,
        CancellationToken cancellationToken)
    {
        var results = await handler.HandleAsync(
            garageId,
            query,
            cancellationToken);

        return Ok(results);
    }
}