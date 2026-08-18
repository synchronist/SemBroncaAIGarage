using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class PostgreSqlReadinessCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<GarageDbContext>().Database;
            return await database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

public sealed class ApiOperationalMiddleware(RequestDelegate next, ILogger<ApiOperationalMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context.Request.Headers[HeaderName].FirstOrDefault(), context.TraceIdentifier);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            ["GarageId"] = context.User.FindFirst("garage_id")?.Value
        });
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected API error for {Method} {Path}", context.Request.Method, context.Request.Path);
            throw;
        }
    }

    public static string Resolve(string? requested, string fallback) =>
        !string.IsNullOrWhiteSpace(requested) && requested.Length <= 64 && requested.All(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            ? requested
            : fallback;
}
