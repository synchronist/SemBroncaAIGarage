using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class CustomerService
{
    private readonly HttpClient _httpClient;
    public CustomerService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<CustomerListModel> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var url = $"api/customers?search={Uri.EscapeDataString(search?.Trim() ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return await _httpClient.GetFromJsonAsync<CustomerListModel>(url, cancellationToken)
            ?? new CustomerListModel(page, pageSize, 0, 0, []);
    }

    public async Task<CustomerDetailsModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/customers/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDetailsModel>(cancellationToken: cancellationToken);
    }

    public Task<SaveCustomerResponse> CreateAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/customers", request, cancellationToken);

    public Task<SaveCustomerResponse> UpdateAsync(Guid id, SaveCustomerRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/customers/{id}", request, cancellationToken);

    private async Task<SaveCustomerResponse> SendAsync(HttpMethod method, string url, SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(request)
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Não foi possível salvar o cliente.");
        }

        return await response.Content.ReadFromJsonAsync<SaveCustomerResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("A API não retornou os dados do cliente.");
    }

    private sealed record ApiError(string Message);
}
