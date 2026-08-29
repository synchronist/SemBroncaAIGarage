namespace SemBroncaAI.Garage.Web.Services;

public sealed class WebOperationalMiddleware(RequestDelegate next, ILogger<WebOperationalMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        var requested = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(requested) && requested.Length <= 64 &&
                            requested.All(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            ? requested
            : context.TraceIdentifier;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            ["GarageId"] = context.User.FindFirst("garage_id")?.Value
        });
        try { await next(context); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected Web error for {Method} {Route}", context.Request.Method, ResolveRoute(context));
            throw;
        }
        finally
        {
            logger.LogInformation("Web request {Method} {Route} returned {StatusCode} in {ElapsedMs:F1} ms",
                context.Request.Method, ResolveRoute(context), context.Response.StatusCode,
                System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }

    private static string ResolveRoute(HttpContext context) =>
        (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
}

public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = accessor.HttpContext?.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(correlationId))
            request.Headers.TryAddWithoutValidation(WebOperationalMiddleware.HeaderName, correlationId);
        return base.SendAsync(request, cancellationToken);
    }
}
