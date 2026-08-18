namespace SemBroncaAI.Garage.Web.Services;

public sealed class PlatformHealthService(HttpClient client)
{
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    public async Task<PlatformHealthSnapshot> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);
        try
        {
            if (!await IsHealthyAsync("health/live", timeout.Token))
                return PlatformHealthSnapshot.ApiUnavailable(DateTime.Now);

            var databaseAvailable = await IsHealthyAsync("health/ready", timeout.Token);
            return new(PlatformComponentState.Operational, PlatformComponentState.Operational,
                databaseAvailable ? PlatformComponentState.Available : PlatformComponentState.Unavailable,
                DateTime.Now);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return PlatformHealthSnapshot.ApiUnavailable(DateTime.Now);
        }
    }

    private async Task<bool> IsHealthyAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

public sealed record PlatformHealthSnapshot(
    PlatformComponentState Web,
    PlatformComponentState Api,
    PlatformComponentState Database,
    DateTime CheckedAt)
{
    public static PlatformHealthSnapshot ApiUnavailable(DateTime checkedAt) =>
        new(PlatformComponentState.Operational, PlatformComponentState.Unavailable,
            PlatformComponentState.Unknown, checkedAt);
}

public enum PlatformComponentState
{
    Operational,
    Available,
    Unavailable,
    Unknown
}
