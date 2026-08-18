using SemBroncaAI.Garage.Infrastructure.Identity;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Infrastructure.Identity;

public sealed class DevelopmentIdentitySeederTests
{
    [Fact]
    public async Task Seed_should_be_idempotent_and_create_all_development_profiles_in_same_garage()
    {
        var options = ValidOptions();
        var store = new SeedStore(options.GarageId);
        var seeder = new DevelopmentIdentitySeeder(store);

        await seeder.SeedAsync(options);
        await seeder.SeedAsync(options);

        store.Users.Count.ShouldBe(4);
        store.Users[options.PlatformAdminEmail].GarageId.ShouldBeNull();
        store.Users.Where(entry => entry.Key != options.PlatformAdminEmail).Select(entry => entry.Value)
            .ShouldAllBe(user => user.GarageId == options.GarageId);
        store.Users.Values.ShouldAllBe(user => user.Active && user.EmailConfirmed);
        store.CreatedUsers.ShouldBe(4);
        store.RoleAssignments.ShouldBe(4);
        store.AssignedRoles.ShouldBe(
            [ApplicationRoles.Owner, ApplicationRoles.Receptionist, ApplicationRoles.Mechanic, ApplicationRoles.PlatformAdmin],
            ignoreOrder: true);
        store.Roles.ShouldBe(ApplicationRoles.All, ignoreOrder: true);
    }

    [Fact]
    public async Task Seed_should_not_create_missing_garage()
    {
        var options = ValidOptions();
        var store = new SeedStore(null);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            new DevelopmentIdentitySeeder(store).SeedAsync(options));

        exception.Message.ShouldContain("não existe");
        store.CreatedUsers.ShouldBe(0);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Receptionist")]
    [InlineData("Mechanic")]
    [InlineData("PlatformAdmin")]
    public async Task Seed_should_fail_clearly_without_each_password(string profile)
    {
        var options = ValidOptions();
        switch (profile)
        {
            case "Owner": options.OwnerPassword = string.Empty; break;
            case "Receptionist": options.ReceptionistPassword = string.Empty; break;
            case "Mechanic": options.MechanicPassword = string.Empty; break;
            case "PlatformAdmin": options.PlatformAdminPassword = string.Empty; break;
        }

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            new DevelopmentIdentitySeeder(new SeedStore(options.GarageId)).SeedAsync(options));
        exception.Message.ShouldContain($"IdentitySeed:{profile}Password");
    }

    private static DevelopmentIdentitySeedOptions ValidOptions() => new()
    {
        Enabled = true,
        GarageId = Guid.NewGuid(),
        OwnerName = "Owner Development",
        OwnerEmail = "owner@test.local",
        OwnerUserName = "owner",
        OwnerPassword = "Development123",
        ReceptionistPassword = "Development123",
        MechanicPassword = "Development123",
        PlatformAdminPassword = "Development123"
    };

    private sealed class SeedStore(Guid? existingGarageId) : IDevelopmentIdentitySeedStore
    {
        public HashSet<string> Roles { get; } = [];
        public Dictionary<string, ApplicationUser> Users { get; } = [];
        public HashSet<string> AssignedRoles { get; } = [];
        public int CreatedUsers { get; private set; }
        public int RoleAssignments { get; private set; }

        public Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken) =>
            Task.FromResult(existingGarageId == garageId);

        public Task EnsureRoleAsync(string role, CancellationToken cancellationToken)
        {
            Roles.Add(role);
            return Task.CompletedTask;
        }

        public Task<ApplicationUser?> FindUserAsync(string email, string userName, CancellationToken cancellationToken) =>
            Task.FromResult(Users.GetValueOrDefault(email));

        public Task<ApplicationUser> CreateUserAsync(
            DevelopmentSeedUser seedUser, Guid? garageId, CancellationToken cancellationToken)
        {
            CreatedUsers++;
            var user = garageId is null
                ? ApplicationUser.CreatePlatformAdmin(seedUser.Name, seedUser.Email, seedUser.UserName)
                : ApplicationUser.CreateGarageUser(seedUser.Name, seedUser.Email, seedUser.UserName, garageId.Value);
            user.EmailConfirmed = true;
            Users[seedUser.Email] = user;
            return Task.FromResult(user);
        }

        public Task EnsureRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken)
        {
            if (AssignedRoles.Add(role)) RoleAssignments++;
            return Task.CompletedTask;
        }
    }
}
