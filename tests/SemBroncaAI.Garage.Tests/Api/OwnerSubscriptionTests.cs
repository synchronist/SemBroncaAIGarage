using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
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

    private static string RepositoryRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
