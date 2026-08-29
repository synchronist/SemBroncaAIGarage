using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class ApprovalOptions
{
    public const string SectionName = "Approval";
    public int DefaultValidityDays { get; set; } = 7;
}

public sealed class ApprovalValidityProvider(IOptions<ApprovalOptions> options) : IApprovalValidityProvider
{
    public int DefaultValidityDays => options.Value.DefaultValidityDays;
}
