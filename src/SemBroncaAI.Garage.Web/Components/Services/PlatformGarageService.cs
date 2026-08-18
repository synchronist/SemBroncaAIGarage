using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class PlatformGarageService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public async Task<PlatformDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<PlatformDashboardResponse>("api/platform/garages/dashboard", JsonOptions, cancellationToken)
        ?? throw new InvalidOperationException("A API não retornou o painel administrativo.");

    public async Task<PlatformGarageListResponse> ListAsync(
        string? search, bool? active, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = active.HasValue ? $"&active={active.Value.ToString().ToLowerInvariant()}" : string.Empty;
        return await httpClient.GetFromJsonAsync<PlatformGarageListResponse>(
            $"api/platform/garages?search={Uri.EscapeDataString(search?.Trim() ?? string.Empty)}&page={page}&pageSize={pageSize}{filter}", JsonOptions, cancellationToken)
            ?? new(page, pageSize, 0, 0, []);
    }

    public async Task<PlatformGarageDetailsResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/platform/garages/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformGarageDetailsResponse>(JsonOptions, cancellationToken);
    }

    public async Task<CreatePlatformGarageResponse> CreateAsync(
        CreatePlatformGarageCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/platform/garages", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var validation = await ReadValidationErrorAsync(response, cancellationToken);
            if (validation is not null) throw new PlatformGarageFormValidationException(validation.Message, validation.Errors);
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
        }
        return await response.Content.ReadFromJsonAsync<CreatePlatformGarageResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A API não retornou a oficina criada.");
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/platform/garages/{id}/active", new { active }, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
    }

    public async Task<PlatformSubscriptionResponse> UpdateSubscriptionAsync(Guid id,
        UpdateGarageSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/platform/garages/{id}/subscription", command, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
        return await response.Content.ReadFromJsonAsync<PlatformSubscriptionResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A API não retornou a assinatura atualizada.");
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Message)) return error.Message;
        }
        return "Não foi possível concluir a operação administrativa.";
    }

    private sealed record ApiError(string Message);

    private static async Task<PlatformGarageValidationError?> ReadValidationErrorAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
            return null;
        try
        {
            return await response.Content.ReadFromJsonAsync<PlatformGarageValidationError>(JsonOptions, cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed record PlatformGarageValidationError(string Message, Dictionary<string, string[]> Errors);

public sealed class PlatformGarageFormValidationException(
    string message,
    IReadOnlyDictionary<string, string[]> errors) : Exception(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
