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

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
