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
    IOwnerBillingService billing,
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

    [HttpPost("checkout")]
    public async Task<ActionResult<BillingRedirectResponse>> Checkout(
        [FromBody] CreateCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await billing.CreateCheckoutAsync(command.Cycle, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Não foi possível iniciar o Checkout da Garage {GarageId}.", currentGarage.RequireGarageId());
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("portal")]
    public async Task<ActionResult<BillingRedirectResponse>> Portal(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await billing.CreatePortalAsync(cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Não foi possível abrir o portal da Garage {GarageId}.", currentGarage.RequireGarageId());
            return Conflict(new { message = exception.Message });
        }
    }
}
