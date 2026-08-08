using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class GarageService(HttpClient httpClient)
{
    public async Task<GarageSettingsModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/garages/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GarageSettingsModel>(cancellationToken: cancellationToken);
    }

    public async Task<GarageSettingsModel> UpdateAsync(Guid id, UpdateGarageSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/garages/{id}", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = "Não foi possível salvar as configurações.";
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                try
                {
                    var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
                    if (!string.IsNullOrWhiteSpace(error?.Message)) message = error.Message;
                }
                catch (System.Text.Json.JsonException)
                {
                    // Mantém uma mensagem segura quando a API viola o contrato JSON.
                }
            }
            throw new InvalidOperationException(message);
        }
        return await response.Content.ReadFromJsonAsync<GarageSettingsModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("A API não retornou os dados da oficina.");
    }

    private sealed record ApiError(string Message);
}
