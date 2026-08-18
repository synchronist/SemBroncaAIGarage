using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class GarageService(HttpClient httpClient)
{
    public async Task<GarageSettingsModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/garage/settings", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GarageSettingsModel>(cancellationToken: cancellationToken);
    }

    public async Task<GarageSettingsModel?> GetContextAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/garage/context", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GarageSettingsModel>(cancellationToken: cancellationToken);
    }

    public async Task<GarageBrandingModel?> GetBrandingAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/garage/branding", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GarageBrandingModel>(cancellationToken: cancellationToken);
    }

    public async Task<GarageSettingsModel> UpdateAsync(UpdateGarageSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/garage/settings", request, cancellationToken);
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

    public async Task<GarageSettingsModel> UploadLogoAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(stream); file.Headers.ContentType = new(contentType);
        content.Add(file, "file", fileName);
        var response = await httpClient.PostAsync("api/garage/logo", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.Headers.ContentType?.MediaType == "application/json"
                ? await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken) : null;
            throw new InvalidOperationException(error?.Message ?? "Não foi possível enviar a logo.");
        }
        return await response.Content.ReadFromJsonAsync<GarageSettingsModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("A API não retornou os dados da oficina.");
    }

    public string GetLogoUrl(string? version = null)
    {
        var path = "/auth/garage-logo";
        if (!string.IsNullOrWhiteSpace(version))
            path += $"?v={Uri.EscapeDataString(version)}";

        return path;
    }

    private sealed record ApiError(string Message);
}
