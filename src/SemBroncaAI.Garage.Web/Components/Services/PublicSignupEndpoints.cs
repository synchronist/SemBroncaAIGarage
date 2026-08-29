using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;

namespace SemBroncaAI.Garage.Web.Services;

public static class PublicSignupEndpoints
{
    public const string RateLimitPolicy = "web-public-signup";
    public const int PermitLimit = 3;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public static void ConfigureRateLimiting(RateLimiterOptions options) =>
        options.AddPolicy(RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(context), _ => CreateOptions()));

    public static string GetPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static FixedWindowRateLimiterOptions CreateOptions() => new()
    {
        PermitLimit = PermitLimit,
        Window = Window,
        QueueLimit = 0,
        AutoReplenishment = true
    };

    public static IEndpointRouteBuilder MapPublicSignup(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/signup", SignupAsync)
            .RequireRateLimiting(RateLimitPolicy);
        return endpoints;
    }

    public static async Task<IResult> SignupAsync(HttpContext context, IAntiforgery antiforgery,
        IHttpClientFactory httpClientFactory)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var command = new PublicGarageSignupCommand(
            form["name"].ToString(), form["document"].ToString(), form["phone"].ToString(),
            form["email"].ToString(), form["ownerName"].ToString(), form["ownerEmail"].ToString(),
            string.Equals(form["acceptedTerms"], "true", StringComparison.OrdinalIgnoreCase));

        try
        {
            var client = httpClientFactory.CreateClient("PublicSignupApi");
            using var response = await client.PostAsJsonAsync("api/public/signup", command, context.RequestAborted);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => Results.LocalRedirect("/signup?created=true"),
                HttpStatusCode.BadRequest => Results.LocalRedirect("/signup?error=validation"),
                HttpStatusCode.Conflict => Results.LocalRedirect("/signup?error=invalid"),
                HttpStatusCode.TooManyRequests => Results.LocalRedirect("/signup?error=limited"),
                _ => Results.LocalRedirect("/signup?error=unavailable")
            };
        }
        catch (HttpRequestException)
        {
            return Results.LocalRedirect("/signup?error=unavailable");
        }
    }
}
