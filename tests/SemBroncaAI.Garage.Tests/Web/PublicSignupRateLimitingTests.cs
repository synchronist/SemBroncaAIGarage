using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class PublicSignupRateLimitingTests
{
    [Fact]
    public void Public_boundary_should_partition_distinct_visitors_independently()
    {
        var first = Context("203.0.113.10");
        var second = Context("203.0.113.11");

        PublicSignupEndpoints.GetPartitionKey(first)
            .ShouldNotBe(PublicSignupEndpoints.GetPartitionKey(second));
    }

    [Fact]
    public void Public_header_should_not_select_an_arbitrary_partition()
    {
        var context = Context("203.0.113.10");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.200";

        PublicSignupEndpoints.GetPartitionKey(context).ShouldBe("203.0.113.10");
        PublicSignupRateLimiting.GetPartitionKey(context).ShouldBe("203.0.113.10");
    }

    [Fact]
    public void Abusive_visitor_should_be_limited_at_public_boundary()
    {
        using var limiter = new FixedWindowRateLimiter(PublicSignupEndpoints.CreateOptions());
        for (var attempt = 0; attempt < PublicSignupEndpoints.PermitLimit; attempt++)
            limiter.AttemptAcquire().IsAcquired.ShouldBeTrue();

        limiter.AttemptAcquire().IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public void Api_should_keep_independent_defense_in_depth()
    {
        using var limiter = new ConcurrencyLimiter(PublicSignupRateLimiting.CreateOptions());
        var leases = new List<RateLimitLease>();
        for (var attempt = 0; attempt < PublicSignupRateLimiting.PermitLimit; attempt++)
        {
            var lease = limiter.AttemptAcquire();
            lease.IsAcquired.ShouldBeTrue();
            leases.Add(lease);
        }

        limiter.AttemptAcquire().IsAcquired.ShouldBeFalse();
        PublicSignupRateLimiting.PermitLimit.ShouldBeGreaterThan(PublicSignupEndpoints.PermitLimit);
        foreach (var lease in leases) lease.Dispose();
    }

    [Fact]
    public async Task Normal_signup_should_be_forwarded_by_the_public_boundary()
    {
        var context = Context("203.0.113.10");
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            "name=Oficina&document=11222333000181&phone=11999999999&email=oficina%40test.local" +
            "&ownerName=Owner&ownerEmail=owner%40test.local&acceptedTerms=true"));
        var client = new HttpClient(new SuccessHandler()) { BaseAddress = new Uri("https://api.test/") };

        var result = await PublicSignupEndpoints.SignupAsync(context, new Antiforgery(), new ClientFactory(client));

        var redirect = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult>();
        redirect.Url.ShouldBe("/signup?created=true");
        redirect.AcceptLocalUrlOnly.ShouldBeTrue();
    }

    private static DefaultHttpContext Context(string address)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);
        return context;
    }

    private sealed class SuccessHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class Antiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => throw new NotSupportedException();
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => throw new NotSupportedException();
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void SetCookieTokenAndHeader(HttpContext httpContext) => throw new NotSupportedException();
        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }
}
