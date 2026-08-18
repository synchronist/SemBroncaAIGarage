using System.Net;
using Microsoft.AspNetCore.Http;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class OperationalHardeningTests
{
    [Fact]
    public void Correlation_id_should_accept_safe_values_and_reject_untrusted_values()
    {
        ApiOperationalMiddleware.Resolve("request-ABC_123", "fallback").ShouldBe("request-ABC_123");
        ApiOperationalMiddleware.Resolve("token=value&password=secret", "fallback").ShouldBe("fallback");
        ApiOperationalMiddleware.Resolve(new string('A', 65), "fallback").ShouldBe("fallback");
    }

    [Fact]
    public async Task Web_to_api_handler_should_propagate_current_correlation_id()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "correlation-42" };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var capture = new CaptureHandler();
        using var handler = new CorrelationIdHandler(accessor) { InnerHandler = capture };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        capture.CorrelationId.ShouldBe("correlation-42");
    }

    [Fact]
    public void Audit_metadata_should_be_bounded_and_contain_no_credential_fields()
    {
        var entry = new AuditEntryEntity(DateTime.UtcNow, Guid.NewGuid(), "Owner", Guid.NewGuid(),
            "member.invited", "ApplicationUser", Guid.NewGuid().ToString("D"), new string('x', 700));

        entry.Summary!.Length.ShouldBe(500);
        var propertyNames = typeof(AuditEntryEntity).GetProperties().Select(x => x.Name).ToArray();
        foreach (var forbidden in new[] { "Password", "Token", "Cookie", "Authorization", "ConnectionString" })
            propertyNames.ShouldNotContain(forbidden);
    }

    [Fact]
    public void Operational_endpoints_and_invitation_prevalidation_should_be_mapped_safely()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var apiProgram = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Api", "Program.cs"));
        var webProgram = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web", "Program.cs"));
        var invitationPage = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web", "Components", "Pages", "AcceptInvitation.razor"));

        apiProgram.ShouldContain("/health/live");
        apiProgram.ShouldContain("/health/ready");
        apiProgram.ShouldContain("PostgreSqlReadinessCheck");
        apiProgram.ShouldContain("options.FallbackPolicy = activeUserPolicy");
        HealthMapping(apiProgram, "/health/live").ShouldContain("AllowAnonymous()");
        HealthMapping(apiProgram, "/health/ready").ShouldContain("AllowAnonymous()");
        webProgram.ShouldContain("/health/live");
        webProgram.ShouldContain("/health/ready");
        HealthMapping(webProgram, "/health/live").ShouldContain("AllowAnonymous()");
        HealthMapping(webProgram, "/health/ready").ShouldContain("AllowAnonymous()");
        apiProgram.ShouldContain("new { status = report.Status.ToString() }");
        foreach (var sensitiveDetail in new[] { "connectionString", "databaseName", "exception", "stackTrace" })
            HealthResponse(apiProgram).ShouldNotContain(sensitiveDetail, Case.Insensitive);
        invitationPage.ShouldContain("Service.CanAcceptAsync");
        invitationPage.ShouldContain("Este convite não é mais válido.");
        invitationPage.ShouldContain("if (!_canAccept && !_activated)");
        invitationPage.ShouldContain("Service.AcceptAsync");
    }

    [Fact]
    public void Audit_query_should_be_tenant_scoped_and_not_exposed_to_team_roles()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var implementation = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Infrastructure", "Services", "PlatformGarageAdministration.cs"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Api", "Controllers", "PlatformGaragesController.cs"));

        implementation.ShouldContain("Where(x => x.GarageId == id)");
        controller.ShouldContain("Authorize(Policy = PlatformAuthorization.Policy)");
    }

    [Fact]
    public void Sensitive_administrative_mutations_should_emit_controlled_audit_actions()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var platform = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Infrastructure", "Services", "PlatformGarageAdministration.cs"));
        var team = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Infrastructure", "Services", "TeamManagement.cs"));

        foreach (var action in new[] { nameof(AuditActions.GarageCreated), nameof(AuditActions.GarageActivated),
                     nameof(AuditActions.GarageDeactivated), nameof(AuditActions.SubscriptionChanged) })
            platform.ShouldContain(action);
        foreach (var action in new[] { nameof(AuditActions.MemberInvited), nameof(AuditActions.InvitationResent),
                     nameof(AuditActions.MemberActivated), nameof(AuditActions.MemberDeactivated), nameof(AuditActions.MemberRoleChanged) })
            team.ShouldContain(action);
        team.ShouldNotContain("auditWriter.Add(garageId, AuditActions.MemberInvited, \"ApplicationUser\", user.Id.ToString(\"D\"), token");
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? CorrelationId { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CorrelationId = request.Headers.GetValues(WebOperationalMiddleware.HeaderName).Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static string HealthMapping(string program, string route)
    {
        var start = program.IndexOf($"MapHealthChecks(\"{route}\"", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        var end = program.IndexOf(';', start);
        return program[start..(end + 1)];
    }

    private static string HealthResponse(string program)
    {
        var start = program.IndexOf("static Task MinimalHealthResponse", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        return program[start..];
    }
}
