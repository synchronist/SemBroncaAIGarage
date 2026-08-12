using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface IEstimateQueryRepository
{
    Task<ListEstimatesResponse> ListAsync(ListEstimatesQuery query, CancellationToken cancellationToken = default);
}
