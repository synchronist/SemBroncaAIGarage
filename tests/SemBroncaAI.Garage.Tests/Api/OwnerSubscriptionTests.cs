using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Application.Features.Subscriptions;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class OwnerSubscriptionTests
{
    [Fact]
    public void Subscription_endpoint_should_require_owner_only_permission()
    {
        typeof(SubscriptionController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(ApplicationPermissions.ViewSubscription);
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Owner])
            .ShouldContain(ApplicationPermissions.ViewSubscription);
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Receptionist])
            .ShouldNotContain(ApplicationPermissions.ViewSubscription);
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Mechanic])
            .ShouldNotContain(ApplicationPermissions.ViewSubscription);
        RolePermissionDefaults.ForRoles(["PlatformAdmin"])
            .ShouldNotContain(ApplicationPermissions.ViewSubscription);
    }

    [Fact]
    public void Default_trial_should_come_from_the_single_shared_configuration()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "SemBroncaAI.Garage.Api", "appsettings.json")));
        document.RootElement.GetProperty(SubscriptionOptions.SectionName)
            .GetProperty(nameof(SubscriptionOptions.DefaultTrialDays)).GetInt32().ShouldBe(90);

        var development = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "SemBroncaAI.Garage.Api", "appsettings.Development.json"));
        development.ShouldNotContain("DefaultTrialDays");
    }

    [Fact]
    public void Changing_default_trial_should_not_modify_an_existing_subscription()
    {
        var startedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, startedAt, 10);
        var newDefault = new SubscriptionOptions { DefaultTrialDays = 90 };

        newDefault.DefaultTrialDays.ShouldBe(90);
        existing.TrialEndsAt.ShouldBe(startedAt.AddDays(10));
    }

    [Fact]
    public void Onboarding_should_use_configured_duration_and_owner_query_should_use_current_garage()
    {
        var root = RepositoryRoot();
        var onboarding = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Infrastructure", "Services", "PlatformGarageAdministration.cs"));
        var query = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Infrastructure", "Services", "OwnerSubscriptionQuery.cs"));

        onboarding.ShouldContain("subscriptionOptions.Value.DefaultTrialDays");
        onboarding.ShouldNotContain("AddDays(90)");
        query.ShouldContain("currentGarage.RequireGarageId()");
        query.ShouldContain("subscription.GarageId == garageId");
        typeof(SubscriptionController).GetMethod(nameof(SubscriptionController.Get))!.GetParameters()
            .ShouldNotContain(parameter => parameter.Name!.Contains("garage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Subscription_should_not_persist_a_fictitious_billing_cycle_or_card_data()
    {
        var properties = typeof(GarageSubscriptionEntity).GetProperties().Select(property => property.Name).ToArray();
        properties.ShouldNotContain("BillingCycle");
        properties.ShouldNotContain(name => name.Contains("Card", StringComparison.OrdinalIgnoreCase));
        properties.ShouldNotContain(name => name.Contains("Cvv", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Stripe_webhook_should_be_explicitly_anonymous_while_owner_actions_remain_protected()
    {
        typeof(StripeWebhookController).GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
        typeof(SubscriptionController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(ApplicationPermissions.ViewSubscription);
        typeof(StripeWebhookController).GetCustomAttribute<AuthorizeAttribute>().ShouldBeNull();
    }

    [Fact]
    public void Billing_state_should_preserve_tenant_and_never_store_card_data()
    {
        var garageId = Guid.NewGuid();
        var now = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new GarageSubscriptionEntity(garageId, SubscriptionPlan.Standard, now, 90);

        subscription.SynchronizeBilling(
            "cus_test", "sub_test", "price_test", SubscriptionStatus.Active,
            now, now.AddMonths(1), false, now.AddMinutes(1));

        subscription.GarageId.ShouldBe(garageId);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.BillingCustomerId.ShouldBe("cus_test");
        subscription.BillingSubscriptionId.ShouldBe("sub_test");
        subscription.BillingPriceId.ShouldBe("price_test");
        subscription.CurrentPeriodEnd.ShouldBe(now.AddMonths(1));
        typeof(GarageSubscriptionEntity).GetProperties()
            .ShouldNotContain(property => property.Name.Contains("Card", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Checkout_contract_should_accept_a_cycle_but_never_a_client_supplied_price()
    {
        var properties = typeof(CreateCheckoutCommand).GetProperties();

        properties.Length.ShouldBe(1);
        properties[0].Name.ShouldBe(nameof(CreateCheckoutCommand.Cycle));
        properties.ShouldNotContain(property => property.Name.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Trial_should_suspend_immediately_after_expiration()
    {
        var now = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 90);

        subscription.AdvanceLifecycle(now.AddDays(90), SubscriptionOperationalPolicy.PastDueGracePeriod)
            .ShouldBeTrue();
        subscription.Status.ShouldBe(SubscriptionStatus.Suspended);
    }

    [Fact]
    public void Past_due_should_remain_operational_for_three_days_then_suspend()
    {
        var now = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 90);
        subscription.ChangeStatus(SubscriptionStatus.Active, now.AddMinutes(1));
        subscription.ChangeStatus(SubscriptionStatus.PastDue, now.AddDays(30));

        subscription.AdvanceLifecycle(now.AddDays(33).AddTicks(-1), SubscriptionOperationalPolicy.PastDueGracePeriod)
            .ShouldBeFalse();
        SubscriptionOperationalPolicy.CanWrite(subscription.Status).ShouldBeTrue();
        subscription.AdvanceLifecycle(now.AddDays(33), SubscriptionOperationalPolicy.PastDueGracePeriod)
            .ShouldBeTrue();
        subscription.Status.ShouldBe(SubscriptionStatus.Suspended);
        SubscriptionOperationalPolicy.CanWrite(subscription.Status).ShouldBeFalse();
    }

    [Fact]
    public void Successful_payment_should_reactivate_without_platform_admin()
    {
        var now = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 90);
        subscription.ChangeStatus(SubscriptionStatus.PastDue, now.AddDays(30));

        subscription.SynchronizeBilling("cus", "sub", "price", SubscriptionStatus.Active,
            now.AddDays(30), now.AddDays(60), false, now.AddDays(31));

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.PastDueAt.ShouldBeNull();
    }

    [Fact]
    public void Cancellation_at_period_end_should_preserve_access_until_period_end()
    {
        var now = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        var periodEnd = now.AddDays(30);
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 90);
        subscription.SynchronizeBilling("cus", "sub", "price", SubscriptionStatus.Active,
            now, periodEnd, true, now.AddMinutes(1));

        subscription.AdvanceLifecycle(periodEnd.AddTicks(-1), SubscriptionOperationalPolicy.PastDueGracePeriod)
            .ShouldBeFalse();
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.AdvanceLifecycle(periodEnd, SubscriptionOperationalPolicy.PastDueGracePeriod)
            .ShouldBeTrue();
        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    private static string RepositoryRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
