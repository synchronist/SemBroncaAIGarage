using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.TeamManagement;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Integration;

public sealed class TeamInvitationPostgresTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("SBGARAGE_INTEGRATION_CONNECTION");

    [PostgresFact, Trait("Category", "PostgreSQLIntegration")]
    public async Task Invite_should_commit_user_role_and_invitation_only_after_email_succeeds()
    {
        await RunScenario(false, true);
        await RunScenario(true, false);
    }

    private static async Task RunScenario(bool failDelivery, bool shouldPersist)
    {
        var sender = new Sender { Fail = failDelivery };
        await using var provider = CreateProvider(sender);
        var suffix = Guid.NewGuid().ToString("N");
        Guid garageId;
        await using (var setup = provider.CreateAsyncScope())
        {
            var context = setup.ServiceProvider.GetRequiredService<GarageDbContext>();
            var garage = new GarageEntity($"Equipe {suffix[..6]}", suffix[..14], "11999999999", $"garage-{suffix}@test.local");
            context.Garages.Add(garage); await context.SaveChangesAsync(); garageId = garage.Id;
            var roles = setup.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            foreach (var role in new[] { ApplicationRoles.Owner, ApplicationRoles.Receptionist }) if (!await roles.RoleExistsAsync(role)) (await roles.CreateAsync(new(role))).Succeeded.ShouldBeTrue();
            var users = setup.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = ApplicationUser.CreateGarageUser("Owner", $"owner-{suffix}@test.local", $"owner-{suffix}", garageId);
            (await users.CreateAsync(owner)).Succeeded.ShouldBeTrue(); (await users.AddToRoleAsync(owner, ApplicationRoles.Owner)).Succeeded.ShouldBeTrue();
            var current = setup.ServiceProvider.GetRequiredService<Current>(); current.GarageId = garageId; current.UserId = owner.Id;
        }
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var team = scope.ServiceProvider.GetRequiredService<ITeamManagement>();
            var email = $"member-{suffix}@test.local";
            var result = await team.InviteAsync(new("Membro", email, $"member-{suffix}", ApplicationRoles.Receptionist));
            result.Succeeded.ShouldBe(shouldPersist);
            var context = scope.ServiceProvider.GetRequiredService<GarageDbContext>();
            (await context.Users.AnyAsync(x => x.Email == email)).ShouldBe(shouldPersist);
            (await context.TeamInvitations.AnyAsync(x => x.GarageId == garageId)).ShouldBe(shouldPersist);
        }
        finally
        {
            await using var cleanup = provider.CreateAsyncScope(); var db = cleanup.ServiceProvider.GetRequiredService<GarageDbContext>();
            await db.TeamInvitations.Where(x => x.GarageId == garageId).ExecuteDeleteAsync(); await db.Users.Where(x => x.GarageId == garageId).ExecuteDeleteAsync(); await db.Garages.Where(x => x.Id == garageId).ExecuteDeleteAsync();
        }
    }

    private static ServiceProvider CreateProvider(Sender sender)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:DefaultConnection"] = ConnectionString, ["App:PublicBaseUrl"] = "https://integration.test", ["Subscription:DefaultTrialDays"] = "90" }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddDataProtection(); services.AddSingleton<IConfiguration>(configuration); services.AddSingleton<Current>(); services.AddSingleton<ICurrentUser>(x=>x.GetRequiredService<Current>()); services.AddSingleton<ICurrentGarage>(x=>x.GetRequiredService<Current>()); services.AddSingleton<ITeamInvitationSender>(sender); services.AddInfrastructure(configuration); return services.BuildServiceProvider();
    }
    private sealed class Current : ICurrentUser, ICurrentGarage { public Guid UserId { get; set; } public Guid? GarageId { get; set; } public bool IsAuthenticated=>true; public bool IsPlatformAdmin=>false; public IReadOnlyCollection<string> Roles { get; }=[ApplicationRoles.Owner]; public Guid RequireGarageId()=>GarageId??throw new InvalidOperationException(); }
    private sealed class Sender : ITeamInvitationSender { public bool Fail { get; init; } public Task SendAsync(TeamInvitationEmail invitation,CancellationToken token)=>Fail?Task.FromException(new InvalidOperationException("simulated")):Task.CompletedTask; }
}
