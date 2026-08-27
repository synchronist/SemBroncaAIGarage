using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemBroncaAI.Garage.Application.Features.Subscriptions;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class OwnerSubscriptionService(HttpClient client)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public Task<OwnerSubscriptionResponse?> GetAsync(CancellationToken cancellationToken = default) =>
        client.GetFromJsonAsync<OwnerSubscriptionResponse>("api/subscription", JsonOptions, cancellationToken);

    public Task<BillingRedirectResponse> CreateCheckoutAsync(
        BillingCycle cycle,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/subscription/checkout", new CreateCheckoutCommand(cycle), cancellationToken);

    public Task<BillingRedirectResponse> CreatePortalAsync(CancellationToken cancellationToken = default) =>
        PostAsync("api/subscription/portal", new { }, cancellationToken);

    private async Task<BillingRedirectResponse> PostAsync(
        string path,
        object body,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BillingRedirectResponse>(JsonOptions, cancellationToken))!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
