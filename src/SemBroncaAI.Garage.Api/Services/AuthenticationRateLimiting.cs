using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SemBroncaAI.Garage.Api.Services;

public static class AuthenticationRateLimiting
{
    public const string LoginPolicy = "login";
    public const string PasswordRecoveryPolicy = "password-recovery";

    public static void AddLoginPolicy(RateLimiterOptions options)
    {
        options.OnRejected = async (context, cancellationToken) =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("AuthenticationRateLimiting");
            logger.LogWarning(
                "Authentication rate limit rejected {Path} from {RemoteIpAddress} with correlation {CorrelationId}.",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.TraceIdentifier);
            context.HttpContext.Response.Headers.RetryAfter = "60";
            await ValueTask.CompletedTask;
        };
        options.AddPolicy(LoginPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        options.AddPolicy(PasswordRecoveryPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0
                }));
    }
}
