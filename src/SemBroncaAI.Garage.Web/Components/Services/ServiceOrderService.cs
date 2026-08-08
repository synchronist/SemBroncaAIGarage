using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class ServiceOrderService
{
    private readonly HttpClient _httpClient;

    public ServiceOrderService(
        HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateServiceOrderResult> CreateAsync(
        CreateServiceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/service-orders",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<CreateServiceOrderResult>(
                    cancellationToken: cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                "A API não retornou os dados da ordem de serviço.");
    }

    public async Task<ServiceOrderDetailsModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/service-orders/{id}",
            cancellationToken);

        if (response.StatusCode ==
            System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ServiceOrderDetailsModel>(
                cancellationToken: cancellationToken);
    }

    public async Task ExecuteTransitionAsync(
        Guid id,
        string action,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/service-orders/{id}/{action}",
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task<ServiceOrderListModel> ListAsync(
        Guid garageId,
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"garageId={garageId}",
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            parameters.Add(
                $"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            parameters.Add(
                $"status={Uri.EscapeDataString(status)}");
        }

        var url =
            $"api/service-orders?{string.Join("&", parameters)}";

        var response = await _httpClient.GetAsync(
            url,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<ServiceOrderListModel>(
                    cancellationToken: cancellationToken);

        return result
            ?? new ServiceOrderListModel(
                page,
                pageSize,
                0,
                0,
                []);
    }

    public async Task<SaveDiagnosisResponse> SaveDiagnosisAsync(
        Guid serviceOrderId,
        string description,
        string? internalNotes,
        CancellationToken cancellationToken = default)
    {
        var request =
            new SaveDiagnosisRequest(
                description,
                internalNotes);

        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/service-orders/{serviceOrderId}/diagnosis",
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<SaveDiagnosisResponse>(
                    cancellationToken: cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                "A API não retornou o diagnóstico salvo.");
    }
}