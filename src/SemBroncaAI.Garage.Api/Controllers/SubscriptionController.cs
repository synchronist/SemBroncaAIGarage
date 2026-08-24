using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.Subscriptions;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize(Policy = ApplicationPermissions.ViewSubscription)]
public sealed class SubscriptionController(
    IOwnerSubscriptionQuery subscription,
    ICurrentGarage currentGarage,
    ILogger<SubscriptionController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OwnerSubscriptionResponse>> Get(CancellationToken cancellationToken)
    {
        if (await subscription.GetAsync(cancellationToken) is { } result)
            return Ok(result);

        logger.LogWarning(
            "Garage {GarageId} não possui GarageSubscription ao consultar a assinatura do Owner.",
            currentGarage.RequireGarageId());
        return NotFound();
    }
}
