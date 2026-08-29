using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SemBroncaAI.Garage.Api.Services;

public static class PublicSignupRateLimiting
{
    public const string PolicyName = "public-signup";
    public const int PermitLimit = 10;

    public static void Configure(RateLimiterOptions options) =>
        options.AddPolicy(PolicyName, context => RateLimitPartition.GetConcurrencyLimiter(
            GetPartitionKey(context), _ => CreateOptions()));

    public static string GetPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static ConcurrencyLimiterOptions CreateOptions() => new()
    {
        PermitLimit = PermitLimit,
        QueueLimit = 0
    };
}
