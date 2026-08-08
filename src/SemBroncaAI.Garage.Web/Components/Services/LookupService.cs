using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class LookupService
{
    private readonly HttpClient _httpClient;

    public LookupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<LookupResult>> SearchAsync(
        Guid garageId,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (query.Length < 3)
        {
            return [];
        }

        var encodedQuery = Uri.EscapeDataString(query);

        var url =
            $"api/lookup?garageId={garageId}&query={encodedQuery}";

        var result =
            await _httpClient.GetFromJsonAsync<List<LookupResult>>(
                url,
                cancellationToken);

        return result ?? [];
    }
}