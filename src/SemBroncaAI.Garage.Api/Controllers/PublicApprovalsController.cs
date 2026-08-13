using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Abstractions.Storage;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/public/approvals")]
[AllowAnonymous]
[EnableRateLimiting("public-approval")]
public sealed class PublicApprovalsController : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token, [FromServices] PublicApprovalHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.GetAsync(token, $"api/public/approvals/{Uri.EscapeDataString(token)}/logo", cancellationToken);
        return response is null ? NotFound(new { message = "Link de aprovação inválido." }) : Ok(response);
    }

    [HttpPost("{token}/approve")]
    public Task<IActionResult> Approve(string token, [FromBody] ApprovalDecisionRequest request,
        [FromServices] PublicApprovalHandler handler, CancellationToken cancellationToken) =>
        Respond(() => handler.RespondAsync(token, true, request, cancellationToken));

    [HttpPost("{token}/reject")]
    public Task<IActionResult> Reject(string token, [FromBody] ApprovalDecisionRequest request,
        [FromServices] PublicApprovalHandler handler, CancellationToken cancellationToken) =>
        Respond(() => handler.RespondAsync(token, false, request, cancellationToken));

    [HttpGet("{token}/logo")]
    public async Task<IActionResult> Logo(string token, [FromServices] IApprovalTokenService tokenService,
        [FromServices] IServiceOrderRepository repository, [FromServices] IBrandAssetStorage storage,
        [FromServices] PublicApprovalHandler approvalHandler,
        CancellationToken cancellationToken)
    {
        if (await approvalHandler.GetAsync(token, null, cancellationToken) is null) return NotFound();
        var order = await repository.GetByApprovalTokenHashAsync(tokenService.Hash(token), cancellationToken);
        if (order?.Garage.LogoStorageKey is null) return NotFound();
        var asset = await storage.OpenAsync(order.Garage.LogoStorageKey, cancellationToken);
        return asset is null ? NotFound() : File(asset.Content, asset.ContentType);
    }

    private static async Task<IActionResult> Respond(Func<Task<EstimateApprovalStatus?>> action)
    {
        try
        {
            var status = await action();
            return status is null ? new NotFoundObjectResult(new { message = "Link de aprovação inválido." }) : new OkObjectResult(new { status });
        }
        catch (InvalidOperationException exception)
        {
            return new BadRequestObjectResult(new { message = exception.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return new ConflictObjectResult(new { message = "Este orçamento já recebeu uma resposta." });
        }
    }
}
