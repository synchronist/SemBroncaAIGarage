using SemBroncaAI.Garage.Application.Abstractions.Persistence;

namespace SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

public sealed class ListEstimatesHandler(IEstimateQueryRepository repository)
{
    public Task<ListEstimatesResponse> HandleAsync(ListEstimatesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.GarageId == Guid.Empty)
            throw new ArgumentException("A oficina deve ser informada.", nameof(query));
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(query), "A paginação informada é inválida.");

        return repository.ListAsync(query, cancellationToken);
    }
}
