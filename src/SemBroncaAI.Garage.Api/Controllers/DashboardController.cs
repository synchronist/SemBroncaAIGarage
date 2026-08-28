using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.Dashboard;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = ApplicationPermissions.ViewServiceOrders)]
public sealed class DashboardController(ICurrentGarage currentGarage, IOperationalDashboardQuery query) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await query.GetAsync(
        currentGarage.RequireGarageId(),
        User.HasClaim(ApplicationPermissions.ClaimType, ApplicationPermissions.ViewEstimateValues),
        DateTimeOffset.UtcNow,
        cancellationToken));
}
