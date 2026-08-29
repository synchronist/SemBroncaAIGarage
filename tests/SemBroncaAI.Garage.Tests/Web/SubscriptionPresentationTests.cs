using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Web.Models;
using Shouldly;
using System.Net;
using SemBroncaAI.Garage.Web.Services;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class SubscriptionPresentationTests
{
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(SubscriptionStatus.Trial, "Período gratuito")]
    [InlineData(SubscriptionStatus.Active, "Assinatura ativa")]
    [InlineData(SubscriptionStatus.PastDue, "Aguardando regularização")]
    [InlineData(SubscriptionStatus.Suspended, "Assinatura suspensa")]
    [InlineData(SubscriptionStatus.Cancelled, "Assinatura cancelada")]
    public void Status_should_have_friendly_copy(SubscriptionStatus status, string expected) =>
        SubscriptionPresentation.StatusLabel(status, Now.AddDays(5), Now).ShouldBe(expected);

    [Fact]
    public void Active_trial_should_show_remaining_days()
    {
        SubscriptionPresentation.RemainingTrialDays(Now.AddDays(89).AddHours(1), Now).ShouldBe(90);
        SubscriptionPresentation.StatusDescription(SubscriptionStatus.Trial, Now.AddDays(2), Now)
            .ShouldBe("Restam 2 dias do seu período gratuito.");
    }

    [Theory]
    [InlineData(0, 90)]
    [InlineData(1, 89)]
    [InlineData(45, 45)]
    [InlineData(89, 1)]
    [InlineData(90, 0)]
    [InlineData(91, 0)]
    public void Remaining_days_should_be_derived_from_persisted_end_and_current_time(int elapsedDays, int expected) =>
        SubscriptionPresentation.RemainingTrialDays(Now.AddDays(90), Now.AddDays(elapsedDays)).ShouldBe(expected);

    [Fact]
    public void Trial_expires_exactly_at_end_timestamp()
    {
        var end = Now.AddDays(90);
        SubscriptionPresentation.IsTrialExpired(end, end.AddTicks(-1)).ShouldBeFalse();
        SubscriptionPresentation.IsTrialExpired(end, end).ShouldBeTrue();
        SubscriptionPresentation.RemainingTrialDays(end, end).ShouldBe(0);
    }

    [Fact]
    public void Expired_trial_should_be_explicit_without_blocking_logic()
    {
        var endedAt = Now.AddDays(-1);
        SubscriptionPresentation.StatusLabel(SubscriptionStatus.Trial, endedAt, Now)
            .ShouldBe("Período gratuito encerrado");
        SubscriptionPresentation.StatusDescription(SubscriptionStatus.Trial, endedAt, Now)
            .ShouldBe("Seu período gratuito terminou em 18/08/2026.");
        SubscriptionPresentation.RemainingTrialDays(endedAt, Now).ShouldBe(0);
    }

    [Fact]
    public void Subscription_page_should_expose_real_checkout_and_portal_actions()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web", "Components", "Pages", "Subscription.razor"));

        page.ShouldContain("StartCheckoutAsync(BillingCycle.Monthly)");
        page.ShouldContain("StartCheckoutAsync(BillingCycle.Annual)");
        page.ShouldContain("OpenPortalAsync");
        page.ShouldContain("forceLoad: true");
        page.ShouldNotContain("Disponível em breve");
    }

    [Fact]
    public async Task Subscription_client_should_read_string_enums_returned_by_the_api()
    {
        using var client = new HttpClient(new SubscriptionResponseHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var result = await new OwnerSubscriptionService(client).GetAsync();

        result.ShouldNotBeNull();
        result.Plan.ShouldBe(SubscriptionPlan.Standard);
        result.Status.ShouldBe(SubscriptionStatus.Trial);
    }

    private sealed class SubscriptionResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"plan":"Standard","status":"Trial","startedAt":"2026-08-19T12:00:00Z","trialEndsAt":"2026-11-17T12:00:00Z","currentPeriodStart":null,"currentPeriodEnd":null}
                """, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
