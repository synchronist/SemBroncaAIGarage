using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Subscriptions;
using Stripe;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/billing/stripe/webhook")]
[AllowAnonymous]
public sealed class StripeWebhookController(
    IBillingWebhookProcessor processor,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature)) return BadRequest();
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        try
        {
            await processor.ProcessAsync(payload, signature, cancellationToken);
            return Ok();
        }
        catch (StripeException exception)
        {
            logger.LogWarning("Webhook Stripe rejeitado: {StripeErrorType}.", exception.StripeError?.Type ?? "signature");
            return BadRequest();
        }
    }
}
