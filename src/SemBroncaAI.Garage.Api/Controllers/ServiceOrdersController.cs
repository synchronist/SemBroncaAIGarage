using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.CancelServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.CreateServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.DeliverServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.FinishService;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ResumeService;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveDiagnosis;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveEstimate;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.SendForApproval;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.StartDiagnosis;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.StartService;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.WaitForParts;
using SemBroncaAI.Garage.Api.Services;

using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/service-orders")]
public sealed class ServiceOrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceOrderCommand command,
        [FromServices] CreateServiceOrderHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await handler.HandleAsync(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
    [FromQuery] Guid garageId,
    [FromQuery] string? search,
    [FromQuery] ServiceOrderStatus? status,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] ListServiceOrdersHandler handler,
    CancellationToken cancellationToken)
    {
        var query =
            new ListServiceOrdersQuery(
                garageId,
                search,
                status,
                page,
                pageSize);

        var response =
            await handler.HandleAsync(
                query,
                cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetServiceOrderByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, cancellationToken);

        return response is null
            ? NotFound(new { message = "Ordem de serviço não encontrada." })
            : Ok(response);
    }

    [HttpPut("{id:guid}/diagnosis")]
    public async Task<IActionResult> SaveDiagnosis(
    Guid id,
    [FromBody] SaveDiagnosisCommand command,
    [FromServices] SaveDiagnosisHandler handler,
    CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await handler.HandleAsync(
                    id,
                    command,
                    null,
                    cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{id:guid}/estimate")]
    public async Task<IActionResult> SaveEstimate(
        Guid id,
        [FromBody] SaveEstimateCommand command,
        [FromServices] SaveEstimateHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await handler.HandleAsync(id, command, cancellationToken));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/documents/{documentType}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, string documentType, [FromQuery] Guid garageId,
        [FromServices] GetServiceOrderByIdHandler handler, [FromServices] IDocumentPdfGenerator generator,
        CancellationToken cancellationToken)
    {
        var order = await handler.HandleAsync(id, cancellationToken);
        if (order is null || order.GarageId != garageId) return NotFound(new { message = "Ordem de serviço não encontrada." });
        var estimate = documentType.Equals("estimate", StringComparison.OrdinalIgnoreCase);
        if (!estimate && !documentType.Equals("service-order", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Tipo de documento inválido." });
        if (estimate && order.Estimate is null) return BadRequest(new { message = "A ordem de serviço não possui orçamento." });
        try
        {
            var route = estimate ? $"service-orders/{id}/estimate/print" : $"service-orders/{id}/print";
            var selector = estimate ? ".estimate-document" : ".service-order-document";
            var bytes = await generator.GenerateAsync(route, selector, cancellationToken);
            var prefix = estimate ? "ORCAMENTO" : "OS";
            var fileName = DocumentFileName.Create(prefix, order.Number, order.Vehicle.Plate);
            return File(bytes, "application/pdf", fileName);
        }
        catch
        {
            return StatusCode(500, new { message = "Não foi possível gerar o PDF. Verifique a instalação do Chromium." });
        }
    }

    [HttpPost("{id:guid}/start-diagnosis")]
    public Task<IActionResult> StartDiagnosis(
        Guid id,
        [FromServices] StartDiagnosisHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/send-for-approval")]
    public Task<IActionResult> SendForApproval(
        Guid id,
        [FromServices] SendForApprovalHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/start-service")]
    public Task<IActionResult> StartService(
        Guid id,
        [FromServices] StartServiceHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/revise-estimate")]
    public Task<IActionResult> ReviseEstimate(Guid id, [FromServices] ReviseEstimateHandler handler,
        CancellationToken cancellationToken) => ExecuteTransition(async () =>
        { await handler.HandleAsync(id, cancellationToken); return new { status = "Diagnosis" }; });

    [HttpPost("{id:guid}/wait-for-parts")]
    public Task<IActionResult> WaitForParts(
        Guid id,
        [FromServices] WaitForPartsHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/resume-service")]
    public Task<IActionResult> ResumeService(
        Guid id,
        [FromServices] ResumeServiceHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/finish")]
    public Task<IActionResult> Finish(
        Guid id,
        [FromServices] FinishServiceHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/deliver")]
    public Task<IActionResult> Deliver(
        Guid id,
        [FromServices] DeliverServiceOrderHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(
        Guid id,
        [FromServices] CancelServiceOrderHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            handler.HandleAsync(id, null, cancellationToken));

    private async Task<IActionResult> ExecuteTransition<TResponse>(
        Func<Task<TResponse>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}
