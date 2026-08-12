using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SemBroncaAI.Garage.Api.Services;

public static class AuthenticationRateLimiting
{
    public const string LoginPolicy = "login";

    public static void AddLoginPolicy(RateLimiterOptions options)
    {
        options.AddPolicy(LoginPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
}
