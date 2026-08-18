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
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ArchiveServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetTechnicalHistory;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.RestoreServiceOrder;
using SemBroncaAI.Garage.Api.Services;

using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/service-orders")]
[Authorize(Policy = "TenantUser")]
[ServiceFilter(typeof(ServiceOrderConcurrencyExceptionFilter))]
public sealed class ServiceOrdersController(
    ICurrentGarage currentGarage,
    ICurrentUser currentUser,
    IServiceOrderRepository repository) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = ApplicationPermissions.CreateServiceOrder)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceOrderRequest request,
        [FromServices] CreateServiceOrderHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateServiceOrderCommand(currentGarage.RequireGarageId(), request.VehicleId, request.CustomerComplaint, request.Mileage);
            var response = await handler.HandleAsync(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = ApplicationPermissions.ViewServiceOrders)]
    public async Task<IActionResult> List(
    [FromQuery] string? search,
    [FromQuery] ServiceOrderStatus? status,
    [FromQuery] ServiceOrderArchiveFilter archive,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] ListServiceOrdersHandler handler,
    CancellationToken cancellationToken)
    {
        try
        {
        var query =
            new ListServiceOrdersQuery(
                currentGarage.RequireGarageId(),
                search,
                status,
                archive,
                page,
                pageSize);

        var response =
            await handler.HandleAsync(
                query,
                cancellationToken);

        response = ServiceOrderResponseAuthorization.Filter(
            response,
            User.HasClaim(ApplicationPermissions.ClaimType, ApplicationPermissions.ViewCustomersVehicles));

        return Ok(response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = ApplicationPermissions.ViewServiceOrders)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetServiceOrderByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(id, cancellationToken))
            return NotFound(new { message = "Ordem de serviço não encontrada." });
        var response = await handler.HandleAsync(id, cancellationToken);

        if (response is not null)
            response = ServiceOrderResponseAuthorization.Filter(
                response,
                User.HasClaim(ApplicationPermissions.ClaimType, ApplicationPermissions.ViewEstimateValues),
                User.HasClaim(ApplicationPermissions.ClaimType, ApplicationPermissions.ManageDiagnosis),
                User.HasClaim(ApplicationPermissions.ClaimType, ApplicationPermissions.ViewCustomersVehicles));

        return response is null
            ? NotFound(new { message = "Ordem de serviço não encontrada." })
            : Ok(response);
    }

    [HttpPut("{id:guid}/diagnosis")]
    [Authorize(Policy = ApplicationPermissions.ManageDiagnosis)]
    public async Task<IActionResult> SaveDiagnosis(
    Guid id,
    [FromBody] SaveDiagnosisCommand command,
    [FromServices] SaveDiagnosisHandler handler,
    CancellationToken cancellationToken)
    {
        try
        {
            if (!await HasAccessAsync(id, cancellationToken))
                return NotFound(new { message = "Ordem de serviço não encontrada." });
            var response =
                await handler.HandleAsync(
                    id,
                    command,
                    currentUser.UserId,
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
    [Authorize(Policy = ApplicationPermissions.ManageEstimates)]
    public async Task<IActionResult> SaveEstimate(
        Guid id,
        [FromBody] SaveEstimateCommand command,
        [FromServices] SaveEstimateHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await HasAccessAsync(id, cancellationToken))
                return NotFound(new { message = "Ordem de serviço não encontrada." });
            return Ok(await handler.HandleAsync(id, command, cancellationToken));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/technical-history")]
    [Authorize(Policy = ApplicationPermissions.ManageDiagnosis)]
    public async Task<IActionResult> GetTechnicalHistory(
        Guid id, [FromQuery] int offset, [FromQuery] int pageSize,
        [FromServices] GetTechnicalHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await handler.HandleAsync(
                id, currentGarage.RequireGarageId(), offset, pageSize, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = ApplicationPermissions.ArchiveServiceOrder)]
    public async Task<IActionResult> Archive(Guid id,
        [FromServices] ArchiveServiceOrderHandler handler, CancellationToken cancellationToken)
    {
        try { await handler.HandleAsync(id, currentGarage.RequireGarageId(), cancellationToken); return NoContent(); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = ApplicationPermissions.ArchiveServiceOrder)]
    public async Task<IActionResult> Restore(Guid id,
        [FromServices] RestoreServiceOrderHandler handler, CancellationToken cancellationToken)
    {
        try { await handler.HandleAsync(id, currentGarage.RequireGarageId(), cancellationToken); return NoContent(); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("{id:guid}/documents/{documentType}/pdf")]
    [Authorize(Policy = ApplicationPermissions.ViewEstimateValues)]
    public async Task<IActionResult> DownloadPdf(Guid id, string documentType,
        [FromServices] GetServiceOrderByIdHandler handler, [FromServices] IDocumentPdfGenerator generator,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(id, cancellationToken)) return NotFound(new { message = "Ordem de serviço não encontrada." });
        var order = await handler.HandleAsync(id, cancellationToken);
        if (order is null) return NotFound(new { message = "Ordem de serviço não encontrada." });
        var estimate = documentType.Equals("estimate", StringComparison.OrdinalIgnoreCase);
        if (!estimate && !documentType.Equals("service-order", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Tipo de documento inválido." });
        if (estimate && order.Estimate is null) return BadRequest(new { message = "A ordem de serviço não possui orçamento." });
        try
        {
            var route = estimate ? $"service-orders/{id}/estimate/print" : $"service-orders/{id}/print";
            var selector = estimate ? ".estimate-document" : ".service-order-document";
            var accessToken = Request.Headers.Authorization.ToString()["Bearer ".Length..];
            var bytes = await generator.GenerateAsync(route, selector, accessToken, cancellationToken);
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
    [Authorize(Policy = ApplicationPermissions.ManageDiagnosis)]
    public Task<IActionResult> StartDiagnosis(
        Guid id,
        [FromServices] StartDiagnosisHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/send-for-approval")]
    [Authorize(Policy = ApplicationPermissions.SendEstimateForApproval)]
    public Task<IActionResult> SendForApproval(
        Guid id,
        [FromServices] SendForApprovalHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/start-service")]
    [Authorize(Policy = ApplicationPermissions.StartService)]
    public Task<IActionResult> StartService(
        Guid id,
        [FromServices] StartServiceHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/revise-estimate")]
    [Authorize(Policy = ApplicationPermissions.ManageEstimates)]
    public Task<IActionResult> ReviseEstimate(Guid id, [FromServices] ReviseEstimateHandler handler,
        CancellationToken cancellationToken) => ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, async () =>
            { await handler.HandleAsync(id, currentUser.UserId, cancellationToken); return new { status = "Diagnosis" }; }));

    [HttpPost("{id:guid}/wait-for-parts")]
    [Authorize(Policy = ApplicationPermissions.ChangeServiceExecutionStatus)]
    public Task<IActionResult> WaitForParts(
        Guid id,
        [FromServices] WaitForPartsHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/resume-service")]
    [Authorize(Policy = ApplicationPermissions.ChangeServiceExecutionStatus)]
    public Task<IActionResult> ResumeService(
        Guid id,
        [FromServices] ResumeServiceHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/finish")]
    [Authorize(Policy = ApplicationPermissions.FinishService)]
    public Task<IActionResult> Finish(
        Guid id,
        [FromServices] FinishServiceHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = ApplicationPermissions.DeliverServiceOrder)]
    public Task<IActionResult> Deliver(
        Guid id,
        [FromServices] DeliverServiceOrderHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = ApplicationPermissions.CancelServiceOrder)]
    public Task<IActionResult> Cancel(
        Guid id,
        [FromServices] CancelServiceOrderHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteTransition(() =>
            TenantTransition(id, cancellationToken, () => handler.HandleAsync(id, currentUser.UserId, cancellationToken)));

    private async Task<TResponse> TenantTransition<TResponse>(
        Guid id, CancellationToken cancellationToken, Func<Task<TResponse>> action)
    {
        if (!await HasAccessAsync(id, cancellationToken))
            throw new InvalidOperationException("Ordem de serviço não encontrada.");
        return await action();
    }

    private async Task<bool> HasAccessAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, currentGarage.RequireGarageId(), cancellationToken) is not null;

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

public sealed record CreateServiceOrderRequest(Guid VehicleId, string CustomerComplaint, int Mileage);
