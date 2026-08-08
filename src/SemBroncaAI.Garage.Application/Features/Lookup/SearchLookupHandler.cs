using SemBroncaAI.Garage.Application.Abstractions.Persistence;

namespace SemBroncaAI.Garage.Application.Features.Lookup;

public sealed class SearchLookupHandler
{
    private readonly ILookupRepository _lookupRepository;

    public SearchLookupHandler(
        ILookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;
    }

    public async Task<IReadOnlyList<LookupResultResponse>> HandleAsync(
        Guid garageId,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (garageId == Guid.Empty)
        {
            throw new ArgumentException(
                "A oficina é obrigatória.",
                nameof(garageId));
        }

        query = query?.Trim() ?? string.Empty;

        if (query.Length < 3)
        {
            return [];
        }

        return await _lookupRepository.SearchAsync(
            garageId,
            query,
            cancellationToken: cancellationToken);
    }
}