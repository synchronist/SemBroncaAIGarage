using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class EstimateService(HttpClient httpClient)
{
    public async Task<EstimateListModel> ListAsync(Guid garageId, string? search, string? status,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"garageId={garageId}", $"page={page}", $"pageSize={pageSize}"
        };
        if (!string.IsNullOrWhiteSpace(search)) parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(status)) parameters.Add($"status={Uri.EscapeDataString(status)}");

        var response = await httpClient.GetAsync($"api/estimates?{string.Join('&', parameters)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EstimateListModel>(cancellationToken: cancellationToken)
            ?? new(1, pageSize, 0, 0, [], new(0, 0, 0, 0));
    }
}
