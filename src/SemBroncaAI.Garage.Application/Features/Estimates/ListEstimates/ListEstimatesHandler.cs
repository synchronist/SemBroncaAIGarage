using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Common;

namespace SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

public sealed class ListEstimatesHandler(IEstimateQueryRepository repository)
{
    public Task<ListEstimatesResponse> HandleAsync(ListEstimatesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.GarageId == Guid.Empty)
            throw new ArgumentException("A oficina deve ser informada.", nameof(query));
        PaginationRules.Validate(query.Page, query.PageSize);
        return repository.ListAsync(query, cancellationToken);
    }
}
