using SemBroncaAI.Garage.Infrastructure.Identity;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Infrastructure.Identity;

public sealed class DevelopmentIdentitySeederTests
{
    [Fact]
    public async Task Seed_should_be_idempotent_and_associate_owner_with_configured_garage()
    {
        var options = ValidOptions();
        var store = new SeedStore(options.GarageId);
        var seeder = new DevelopmentIdentitySeeder(store);

        await seeder.SeedAsync(options);
        await seeder.SeedAsync(options);

        store.User.ShouldNotBeNull();
        store.User.GarageId.ShouldBe(options.GarageId);
        store.User.Active.ShouldBeTrue();
        store.User.EmailConfirmed.ShouldBeTrue();
        store.CreatedUsers.ShouldBe(1);
        store.OwnerRoleAssignments.ShouldBe(1);
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
        store.CreatedGarages.ShouldBe(0);
    }

    [Fact]
    public async Task Seed_should_fail_clearly_without_password()
    {
        var options = ValidOptions(); options.OwnerPassword = string.Empty;
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            new DevelopmentIdentitySeeder(new SeedStore(options.GarageId)).SeedAsync(options));
        exception.Message.ShouldContain("IdentitySeed:OwnerPassword");
    }

    private static DevelopmentIdentitySeedOptions ValidOptions() => new()
    {
        Enabled = true,
        GarageId = Guid.NewGuid(),
        OwnerName = "Owner Development",
        OwnerEmail = "owner@test.local",
        OwnerUserName = "owner",
        OwnerPassword = "Development123"
    };

    private sealed class SeedStore(Guid? existingGarageId) : IDevelopmentIdentitySeedStore
    {
        public HashSet<string> Roles { get; } = [];
        public ApplicationUser? User { get; private set; }
        public int CreatedUsers { get; private set; }
        public int OwnerRoleAssignments { get; private set; }
        public int CreatedGarages => 0;

        public Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken) =>
            Task.FromResult(existingGarageId == garageId);
        public Task EnsureRoleAsync(string role, CancellationToken cancellationToken) { Roles.Add(role); return Task.CompletedTask; }
        public Task<ApplicationUser?> FindUserAsync(string email, string userName, CancellationToken cancellationToken) => Task.FromResult(User);
        public Task<ApplicationUser> CreateOwnerAsync(DevelopmentIdentitySeedOptions options, CancellationToken cancellationToken)
        {
            CreatedUsers++;
            User = ApplicationUser.CreateGarageUser(options.OwnerName, options.OwnerEmail, options.OwnerUserName, options.GarageId);
            User.EmailConfirmed = true;
            return Task.FromResult(User);
        }
        public Task EnsureOwnerRoleAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (OwnerRoleAssignments == 0) OwnerRoleAssignments++;
            return Task.CompletedTask;
        }
    }
}
