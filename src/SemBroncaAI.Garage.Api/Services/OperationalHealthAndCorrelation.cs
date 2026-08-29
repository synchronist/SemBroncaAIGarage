using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Domain.Entities.Garage;

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

    public async Task InvokeAsync(HttpContext context, GarageDbContext dbContext)
    {
        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
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
            if (!await AuthorizeSubscriptionOperationAsync(context, dbContext))
                return;
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected API error for {Method} {Route}", context.Request.Method, ResolveRoute(context));
            throw;
        }
        finally
        {
            logger.LogInformation("API request {Method} {Route} returned {StatusCode} in {ElapsedMs:F1} ms",
                context.Request.Method, ResolveRoute(context), context.Response.StatusCode,
                System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }

    private static string ResolveRoute(HttpContext context) =>
        (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";

    private static async Task<bool> AuthorizeSubscriptionOperationAsync(
        HttpContext context,
        GarageDbContext dbContext)
    {
        var garageClaim = context.User.FindFirst("garage_id")?.Value;
        if (!Guid.TryParse(garageClaim, out var garageId)) return true;

        var subscription = await dbContext.GarageSubscriptions
            .SingleOrDefaultAsync(x => x.GarageId == garageId, context.RequestAborted);
        if (subscription is null) return true;

        if (subscription.AdvanceLifecycle(DateTime.UtcNow, SubscriptionOperationalPolicy.PastDueGracePeriod))
            await dbContext.SaveChangesAsync(context.RequestAborted);

        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            IsBillingRecoveryPath(context.Request.Path) ||
            SubscriptionOperationalPolicy.CanWrite(subscription.Status))
            return true;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            code = "subscription-restricted",
            message = "A assinatura da oficina precisa ser regularizada para realizar esta operação."
        }, context.RequestAborted);
        return false;
    }

    private static bool IsBillingRecoveryPath(PathString path) =>
        path.StartsWithSegments("/api/subscription") ||
        path.StartsWithSegments("/api/billing/stripe/webhook");

    public static string Resolve(string? requested, string fallback) =>
        !string.IsNullOrWhiteSpace(requested) && requested.Length <= 64 && requested.All(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            ? requested
            : fallback;
}
