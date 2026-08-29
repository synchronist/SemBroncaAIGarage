using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class EstimateQueryRepository(GarageDbContext context, IApprovalTokenService tokenService)
    : IEstimateQueryRepository
{
    public async Task<ListEstimatesResponse> ListAsync(ListEstimatesQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var source = context.ServiceOrders.AsNoTracking()
            .ApplyOperationalEstimateFilter(query.GarageId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var term = $"%{search}%";
            var number = int.TryParse(search.TrimStart('#'), out var parsed) ? parsed : (int?)null;
            source = source.Where(x =>
                (number.HasValue && x.Number == number.Value) ||
                EF.Functions.ILike(x.Vehicle.Customer.Name, term) ||
                EF.Functions.ILike(x.Vehicle.Customer.Phone, term) ||
                EF.Functions.ILike(x.Vehicle.Plate, term) ||
                EF.Functions.ILike(x.Vehicle.Brand, term) ||
                EF.Functions.ILike(x.Vehicle.Model, term));
        }

        var shaped = source.Select(x => new
        {
            Order = x,
            Approval = x.EstimateApprovals.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).FirstOrDefault()
        });

        if (query.Status.HasValue)
        {
            shaped = query.Status.Value switch
            {
                EstimateCommercialStatus.NotSent => shaped.Where(x => x.Approval == null || x.Approval.InvalidatedAt != null),
                EstimateCommercialStatus.Pending => shaped.Where(x => x.Approval != null && x.Approval.InvalidatedAt == null && x.Approval.Status == EstimateApprovalStatus.Pending && x.Approval.ExpiresAt > now),
                EstimateCommercialStatus.Approved => shaped.Where(x => x.Approval != null && x.Approval.InvalidatedAt == null && x.Approval.Status == EstimateApprovalStatus.Approved),
                EstimateCommercialStatus.PartiallyApproved => shaped.Where(x => x.Approval != null && x.Approval.InvalidatedAt == null && x.Approval.Status == EstimateApprovalStatus.PartiallyApproved),
                EstimateCommercialStatus.Rejected => shaped.Where(x => x.Approval != null && x.Approval.InvalidatedAt == null && x.Approval.Status == EstimateApprovalStatus.Rejected),
                EstimateCommercialStatus.Expired => shaped.Where(x => x.Approval != null && x.Approval.InvalidatedAt == null && x.Approval.Status == EstimateApprovalStatus.Pending && x.Approval.ExpiresAt <= now),
                _ => shaped
            };
        }

        var totalItems = await shaped.CountAsync(cancellationToken);
        var indicators = await source.Select(x => x.EstimateApprovals.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).FirstOrDefault())
            .GroupBy(_ => 1)
            .Select(group => new EstimateIndicators(
                group.Count(a => a != null && a.InvalidatedAt == null && a.Status == EstimateApprovalStatus.Pending && a.ExpiresAt > now),
                group.Count(a => a != null && a.InvalidatedAt == null && (a.Status == EstimateApprovalStatus.Approved || a.Status == EstimateApprovalStatus.PartiallyApproved)),
                group.Count(a => a != null && a.InvalidatedAt == null && a.Status == EstimateApprovalStatus.Rejected),
                group.Where(a => a != null && a.InvalidatedAt == null && a.Status == EstimateApprovalStatus.Pending && a.ExpiresAt > now).Sum(a => a!.EstimateTotal)))
            .FirstOrDefaultAsync(cancellationToken) ?? new EstimateIndicators(0, 0, 0, 0);

        var rows = await shaped.OrderByDescending(x => x.Order.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new
            {
                x.Order.Id,
                x.Order.Number,
                x.Order.Status,
                x.Order.CreatedAt,
                CustomerName = x.Order.Vehicle.Customer.Name,
                CustomerPhone = x.Order.Vehicle.Customer.Phone,
                Vehicle = x.Order.Vehicle.Brand + " " + x.Order.Vehicle.Model,
                x.Order.Vehicle.Plate,
                Total = x.Order.Estimate!.Items.Sum(item => item.Quantity * item.UnitPrice),
                ApprovalStatus = x.Approval == null ? (EstimateApprovalStatus?)null : x.Approval.Status,
                ExpiresAt = x.Approval == null ? (DateTimeOffset?)null : x.Approval.ExpiresAt,
                InvalidatedAt = x.Approval == null ? (DateTimeOffset?)null : x.Approval.InvalidatedAt,
                SentAt = x.Approval == null ? (DateTimeOffset?)null : x.Approval.CreatedAt,
                RespondedAt = x.Approval == null ? null : x.Approval.RespondedAt,
                Comment = x.Approval == null ? null : x.Approval.CustomerComment,
                ProtectedToken = x.Approval == null ? null : x.Approval.ProtectedToken
            }).ToArrayAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var status = EstimateCommercialStatusResolver.Resolve(row.ApprovalStatus, row.ExpiresAt, row.InvalidatedAt, now);
            var token = status == EstimateCommercialStatus.Pending ? TryUnprotect(row.ProtectedToken) : null;
            return new ListEstimatesItem(row.Id, row.Number, row.Status.ToString(), row.CreatedAt,
                row.CustomerName, row.CustomerPhone, row.Vehicle.Trim(), row.Plate, row.Total, status,
                row.SentAt, row.RespondedAt, row.Comment, token);
        }).ToArray();

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        return new(query.Page, query.PageSize, totalItems, totalPages, items, indicators);
    }

    private string? TryUnprotect(string? protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken)) return null;
        try { return tokenService.Unprotect(protectedToken); }
        catch { return null; }
    }
}
