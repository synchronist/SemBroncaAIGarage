using System.Net;
using System.Net.Http.Json;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class PublicApprovalService(HttpClient client)
{
    public async Task<PublicApprovalModel?> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync($"api/public/approvals/{Uri.EscapeDataString(token)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureAsync(response, cancellationToken);
        var model = await response.Content.ReadFromJsonAsync<PublicApprovalModel>(cancellationToken: cancellationToken);
        if (model?.LogoUrl is not null) model = model with { LogoUrl = new Uri(client.BaseAddress!, model.LogoUrl).ToString() };
        return model;
    }

    public async Task RespondAsync(string token, bool approve, PublicApprovalDecision decision,
        CancellationToken cancellationToken = default)
    {
        var action = approve ? "approve" : "reject";
        var response = await client.PostAsJsonAsync($"api/public/approvals/{Uri.EscapeDataString(token)}/{action}", decision, cancellationToken);
        await EnsureAsync(response, cancellationToken);
    }

    private static async Task EnsureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        ApiError? error = null;
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
            error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken);
        throw new InvalidOperationException(error?.Message ?? "Não foi possível processar a aprovação.");
    }
    private sealed record ApiError(string Message);
}
