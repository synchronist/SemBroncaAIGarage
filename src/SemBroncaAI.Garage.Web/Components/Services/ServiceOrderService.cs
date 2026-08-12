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
        string archive = "Active",
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"garageId={garageId}",
            $"page={page}",
            $"pageSize={pageSize}",
            $"archive={Uri.EscapeDataString(archive)}"
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

    public async Task SetArchivedAsync(Guid id, Guid garageId, bool archived,
        CancellationToken cancellationToken = default)
    {
        var action = archived ? "archive" : "restore";
        var response = await _httpClient.PostAsync(
            $"api/service-orders/{id}/{action}?garageId={garageId}", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.Headers.ContentType?.MediaType == "application/json"
                ? await response.Content.ReadFromJsonAsync<PdfError>(cancellationToken: cancellationToken) : null;
            throw new InvalidOperationException(error?.Message ?? "Não foi possível atualizar o arquivamento da OS.");
        }
    }

    public async Task<PdfDownload> DownloadPdfAsync(Guid id, Guid garageId, bool estimate, CancellationToken cancellationToken = default)
    {
        var type = estimate ? "estimate" : "service-order";
        var response = await _httpClient.GetAsync($"api/service-orders/{id}/documents/{type}/pdf?garageId={garageId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.Headers.ContentType?.MediaType == "application/json"
                ? await response.Content.ReadFromJsonAsync<PdfError>(cancellationToken: cancellationToken) : null;
            throw new InvalidOperationException(error?.Message ?? "Não foi possível gerar o PDF.");
        }
        var disposition = response.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"') ?? "documento.pdf";
        return new(await response.Content.ReadAsByteArrayAsync(cancellationToken), fileName);
    }

    private sealed record PdfError(string Message);

    public async Task<SaveEstimateResponse> SaveEstimateAsync(
        Guid serviceOrderId,
        IReadOnlyCollection<SaveEstimateItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/service-orders/{serviceOrderId}/estimate",
            new SaveEstimateRequest(items),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SaveEstimateResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "A API não retornou o orçamento salvo.");
    }
}

public sealed record PdfDownload(byte[] Content, string FileName);
