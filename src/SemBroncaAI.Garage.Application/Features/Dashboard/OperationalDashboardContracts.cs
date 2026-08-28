namespace SemBroncaAI.Garage.Application.Features.Dashboard;

public sealed record OperationalDashboardResponse(
    DashboardCounters Counters,
    IReadOnlyCollection<DashboardAttentionItem> Attention,
    DashboardMonthlySummary? MonthlySummary,
    IReadOnlyCollection<DashboardActivityItem> RecentActivity,
    IReadOnlyCollection<DashboardDailyCompletion> DailyCompletions);

public sealed record DashboardCounters(int Open, int WaitingApproval, int InService, int ReadyForDelivery, int EntriesToday);
public sealed record DashboardAttentionItem(Guid ServiceOrderId, int Number, string Plate, string Reason, DateTimeOffset LastMovementAt);
public sealed record DashboardMonthlySummary(int Completed, decimal ApprovedEstimateValue, decimal AverageTicket,
    decimal ApprovalRate, double? AverageCompletionHours);
public sealed record DashboardActivityItem(Guid ServiceOrderId, int Number, string Plate, string Description,
    string Status, DateTimeOffset OccurredAt);
public sealed record DashboardDailyCompletion(DateOnly Date, int Count);

public interface IOperationalDashboardQuery
{
    Task<OperationalDashboardResponse> GetAsync(Guid garageId, bool includeFinancialMetrics,
        DateTimeOffset now, CancellationToken cancellationToken = default);
}
