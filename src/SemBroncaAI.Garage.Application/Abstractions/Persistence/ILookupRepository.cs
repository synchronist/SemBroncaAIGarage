using SemBroncaAI.Garage.Application.Features.Lookup;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface ILookupRepository
{
    Task<IReadOnlyList<LookupResultResponse>> SearchAsync(
        Guid garageId,
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default);
}