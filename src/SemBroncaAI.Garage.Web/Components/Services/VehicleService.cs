using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;
namespace SemBroncaAI.Garage.Web.Services;
public sealed class VehicleService(HttpClient client)
{
    public async Task<VehicleListModel> ListAsync(string? search, int page, int pageSize, CancellationToken token = default) =>
        await client.GetFromJsonAsync<VehicleListModel>($"api/vehicles?search={Uri.EscapeDataString(search ?? string.Empty)}&page={page}&pageSize={pageSize}", token) ?? new(page, pageSize, 0, 0, []);
    public async Task<VehicleDetailsModel?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        var response = await client.GetAsync($"api/vehicles/{id}", token);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VehicleDetailsModel>(cancellationToken: token);
    }
    public Task<SaveVehicleResponse> CreateAsync(SaveVehicleRequest request, CancellationToken token = default) => SendAsync(HttpMethod.Post, "api/vehicles", request, token);
    public Task<SaveVehicleResponse> UpdateAsync(Guid id, SaveVehicleRequest request, CancellationToken token = default) => SendAsync(HttpMethod.Put, $"api/vehicles/{id}", request, token);
    private async Task<SaveVehicleResponse> SendAsync(HttpMethod method, string url, SaveVehicleRequest request, CancellationToken token)
    {
        var response = await client.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(request) }, token);
        if (!response.IsSuccessStatusCode) { var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: token); throw new InvalidOperationException(error?.Message ?? "Não foi possível salvar o veículo."); }
        return await response.Content.ReadFromJsonAsync<SaveVehicleResponse>(cancellationToken: token) ?? throw new InvalidOperationException("A API não retornou o veículo.");
    }
    private sealed record ApiError(string Message);
}
