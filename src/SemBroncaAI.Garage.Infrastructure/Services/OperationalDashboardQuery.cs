using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Features.Dashboard;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class OperationalDashboardQuery(GarageDbContext context) : IOperationalDashboardQuery
{
    // V1 operational thresholds. They remain local until the product needs per-garage configuration.
    private static readonly TimeSpan WaitingApprovalThreshold = TimeSpan.FromHours(48);
    private static readonly TimeSpan StaleOpenOrderThreshold = TimeSpan.FromDays(7);

    public async Task<OperationalDashboardResponse> GetAsync(Guid garageId, bool includeFinancialMetrics,
        DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var chartStart = today.AddDays(-29);
        var orders = context.ServiceOrders.AsNoTracking().Where(x => x.GarageId == garageId);

        var counters = await orders.GroupBy(_ => 1).Select(group => new DashboardCounters(
            group.Count(x => x.Status != ServiceOrderStatus.Delivered && x.Status != ServiceOrderStatus.Cancelled),
            group.Count(x => x.Status == ServiceOrderStatus.WaitingApproval),
            group.Count(x => x.Status == ServiceOrderStatus.InProgress),
            group.Count(x => x.Status == ServiceOrderStatus.Finished),
            group.Count(x => x.CreatedAt >= today && x.CreatedAt < today.AddDays(1))))
            .SingleOrDefaultAsync(cancellationToken) ?? new(0, 0, 0, 0, 0);

        var approvalLimit = now.Subtract(WaitingApprovalThreshold);
        var staleLimit = now.Subtract(StaleOpenOrderThreshold);
        var attentionRows = await orders
            .Where(order => order.Status == ServiceOrderStatus.WaitingParts ||
                            order.Status == ServiceOrderStatus.Finished ||
                            (order.Status == ServiceOrderStatus.WaitingApproval &&
                             order.History.Max(history => history.CreatedAt) <= approvalLimit) ||
                            (order.Status != ServiceOrderStatus.Delivered && order.Status != ServiceOrderStatus.Cancelled &&
                             order.Status != ServiceOrderStatus.WaitingParts && order.Status != ServiceOrderStatus.Finished &&
                             order.Status != ServiceOrderStatus.WaitingApproval &&
                             order.History.Max(history => history.CreatedAt) <= staleLimit))
            .OrderBy(order => order.History.Max(history => history.CreatedAt))
            .Take(10)
            .Select(order => new
            {
                order.Id, order.Number, order.Vehicle.Plate, order.Status,
                LastMovementAt = order.History.Max(history => history.CreatedAt)
            }).ToArrayAsync(cancellationToken);

        var attention = attentionRows.Select(row => new DashboardAttentionItem(row.Id, row.Number, row.Plate,
            row.Status switch
            {
                ServiceOrderStatus.WaitingApproval => "Aguardando aprovação há mais de 48 horas",
                ServiceOrderStatus.WaitingParts => "Aguardando peças",
                ServiceOrderStatus.Finished => "Pronta para entrega",
                _ => "Sem movimentação há mais de 7 dias"
            }, row.LastMovementAt)).ToArray();

        var activity = await context.ServiceOrderHistories.AsNoTracking()
            .Where(history => history.ServiceOrder.GarageId == garageId)
            .OrderByDescending(history => history.CreatedAt).Take(8)
            .Select(history => new DashboardActivityItem(history.ServiceOrderId, history.ServiceOrder.Number,
                history.ServiceOrder.Vehicle.Plate, history.Description, history.CurrentStatus.ToString(), history.CreatedAt))
            .ToArrayAsync(cancellationToken);

        var completions = await context.ServiceOrderHistories.AsNoTracking()
            .Where(history => history.ServiceOrder.GarageId == garageId &&
                              history.CurrentStatus == ServiceOrderStatus.Finished && history.CreatedAt >= chartStart)
            .GroupBy(history => history.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);
        var completionMap = completions.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var daily = Enumerable.Range(0, 30).Select(offset => DateOnly.FromDateTime(chartStart.AddDays(offset).Date))
            .Select(date => new DashboardDailyCompletion(date, completionMap.GetValueOrDefault(date))).ToArray();

        DashboardMonthlySummary? monthly = null;
        if (includeFinancialMetrics)
        {
            var completedRows = await context.ServiceOrderHistories.AsNoTracking()
                .Where(history => history.ServiceOrder.GarageId == garageId &&
                                  history.CurrentStatus == ServiceOrderStatus.Finished && history.CreatedAt >= monthStart)
                .Select(history => new { history.ServiceOrderId, history.ServiceOrder.CreatedAt, FinishedAt = history.CreatedAt })
                .ToArrayAsync(cancellationToken);
            var tenantOrderIds = orders.Select(order => order.Id);
            var decisions = await context.ServiceOrderEstimateApprovals.AsNoTracking()
                .Where(approval => tenantOrderIds.Contains(approval.ServiceOrderId) && approval.RespondedAt >= monthStart &&
                                   (approval.Status == EstimateApprovalStatus.Approved || approval.Status == EstimateApprovalStatus.PartiallyApproved || approval.Status == EstimateApprovalStatus.Rejected))
                .Select(approval => new { approval.Status, approval.EstimateTotal, approval.ApprovedTotal }).ToArrayAsync(cancellationToken);
            var approved = decisions.Where(x => x.Status is EstimateApprovalStatus.Approved or EstimateApprovalStatus.PartiallyApproved)
                .Select(x => x.Status == EstimateApprovalStatus.PartiallyApproved ? x.ApprovedTotal ?? 0 : x.EstimateTotal).ToArray();
            monthly = new DashboardMonthlySummary(completedRows.Length, approved.Sum(),
                approved.Length == 0 ? 0 : approved.Average(),
                decisions.Length == 0 ? 0 : decimal.Round(approved.Length * 100m / decisions.Length, 1),
                completedRows.Length == 0 ? null : completedRows.Average(x => (x.FinishedAt - x.CreatedAt).TotalHours));
        }

        return new(counters, attention, monthly, activity, daily);
    }
}
