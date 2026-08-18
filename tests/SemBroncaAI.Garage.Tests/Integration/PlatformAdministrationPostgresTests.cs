using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Integration;

public sealed class PlatformAdministrationPostgresTests
{
    private static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable("SBGARAGE_INTEGRATION_CONNECTION");

    [PostgresFact]
    [Trait("Category", "PostgreSQLIntegration")]
    public async Task Onboarding_should_atomically_create_active_garage_and_owner_then_support_admin_queries()
    {
        await using var provider = CreateProvider();
        await EnsureOwnerRoleAsync(provider);
        var suffix = Guid.NewGuid().ToString("N");
        var document = $"PA{suffix}"[..20];
        var ownerEmail = $"owner-{suffix}@test.local";
        Guid garageId = default;
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var administration = scope.ServiceProvider.GetRequiredService<IPlatformGarageAdministration>();
            var created = await administration.CreateAsync(new(
                $"Platform QA {suffix[..6]}", document, "11999999999", $"garage-{suffix}@test.local",
                "Owner QA", ownerEmail, $"owner-{suffix}", "Initial123", "Initial123"));
            garageId = created.GarageId;
            created.Active.ShouldBeTrue();

            var details = await administration.GetByIdAsync(garageId);
            details.ShouldNotBeNull();
            details.UserCount.ShouldBe(1);
            details.OwnerEmail.ShouldBe(ownerEmail);
            var listed = await administration.ListAsync(new(document, true, 1, 20));
            listed.Items.Single().Id.ShouldBe(garageId);
            (await administration.SetActiveAsync(garageId, false)).ShouldBeTrue();
            (await administration.ListAsync(new(document, false, 1, 20))).Items.Single().Active.ShouldBeFalse();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = await userManager.FindByEmailAsync(ownerEmail);
            owner.ShouldNotBeNull();
            owner.GarageId.ShouldBe(garageId);
            owner.Active.ShouldBeTrue();
            (await userManager.IsInRoleAsync(owner, ApplicationRoles.Owner)).ShouldBeTrue();
        }
        finally
        {
            if (garageId != Guid.Empty) await CleanupAsync(provider, garageId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgreSQLIntegration")]
    public async Task Invalid_owner_should_rollback_garage_creation()
    {
        await using var provider = CreateProvider();
        await EnsureOwnerRoleAsync(provider);
        var suffix = Guid.NewGuid().ToString("N");
        var document = $"PR{suffix}"[..20];
        await using (var scope = provider.CreateAsyncScope())
        {
            var administration = scope.ServiceProvider.GetRequiredService<IPlatformGarageAdministration>();
            await Should.ThrowAsync<ArgumentException>(() => administration.CreateAsync(new(
                "Rollback QA", document, "11999999999", $"rollback-{suffix}@test.local",
                "Owner QA", $"rollback-owner-{suffix}@test.local", $"rollback-{suffix}", "short", "short")));
        }

        await using var verifyScope = provider.CreateAsyncScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<GarageDbContext>();
        (await context.Garages.AnyAsync(x => x.Document == document)).ShouldBeFalse();
        (await context.Users.AnyAsync(x => x.Email == $"rollback-owner-{suffix}@test.local")).ShouldBeFalse();
    }

    private static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static async Task EnsureOwnerRoleAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roles.RoleExistsAsync(ApplicationRoles.Owner))
            (await roles.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.Owner))).Succeeded.ShouldBeTrue();
    }

    private static async Task CleanupAsync(ServiceProvider provider, Guid garageId)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<GarageDbContext>();
        await context.Users.Where(x => x.GarageId == garageId).ExecuteDeleteAsync();
        await context.Garages.Where(x => x.Id == garageId).ExecuteDeleteAsync();
    }
}
