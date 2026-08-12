using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Infrastructure.Identity;

public sealed class IdentityFoundationTests
{
    [Fact]
    public void Garage_user_should_require_and_keep_garage_while_platform_admin_has_none()
    {
        var garageId = Guid.NewGuid();
        var owner = ApplicationUser.CreateGarageUser("Owner", "owner@test.local", "owner", garageId);
        var admin = ApplicationUser.CreatePlatformAdmin("Admin", "admin@test.local", "admin");

        owner.GarageId.ShouldBe(garageId);
        owner.Active.ShouldBeTrue();
        owner.Deactivate(); owner.Active.ShouldBeFalse();
        owner.Activate(); owner.Active.ShouldBeTrue();
        admin.GarageId.ShouldBeNull();
        Should.Throw<ArgumentException>(() =>
            ApplicationUser.CreateGarageUser("Owner", "owner@test.local", "owner", Guid.Empty));
    }

    [Fact]
    public void Email_should_be_required_and_identity_indexes_should_be_unique()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ApplicationUser)).ShouldNotBeNull();

        entity.FindProperty(nameof(ApplicationUser.Email))!.IsNullable.ShouldBeFalse();
        entity.GetIndexes().Single(x => x.Properties.Single().Name == nameof(ApplicationUser.NormalizedEmail)).IsUnique.ShouldBeTrue();
        entity.GetIndexes().Single(x => x.Properties.Single().Name == nameof(ApplicationUser.NormalizedUserName)).IsUnique.ShouldBeTrue();
        Should.Throw<ArgumentException>(() =>
            ApplicationUser.CreateGarageUser("Owner", " ", "owner", Guid.NewGuid()));
    }

    [Fact]
    public void Garage_relationship_should_be_optional_and_restrict_delete()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ApplicationUser)).ShouldNotBeNull();
        var foreignKey = entity.GetForeignKeys().Single(x => x.Properties.Single().Name == nameof(ApplicationUser.GarageId));

        foreignKey.IsRequired.ShouldBeFalse();
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Should_configure_unique_email_password_and_lockout()
    {
        var services = new ServiceCollection();
        services.AddOptions<IdentityOptions>().Configure(DependencyInjection.ConfigureIdentity);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        options.User.RequireUniqueEmail.ShouldBeTrue();
        options.Password.RequiredLength.ShouldBe(10);
        options.Password.RequireUppercase.ShouldBeTrue();
        options.Password.RequireLowercase.ShouldBeTrue();
        options.Password.RequireDigit.ShouldBeTrue();
        options.Password.RequireNonAlphanumeric.ShouldBeFalse();
        options.Lockout.AllowedForNewUsers.ShouldBeTrue();
        options.Lockout.MaxFailedAccessAttempts.ShouldBe(5);
        options.Lockout.DefaultLockoutTimeSpan.ShouldBe(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void Password_should_use_identity_password_hasher()
    {
        var user = ApplicationUser.CreateGarageUser("Owner", "owner@test.local", "owner", Guid.NewGuid());
        var hasher = new PasswordHasher<ApplicationUser>();
        const string password = "Development123";
        var hash = hasher.HashPassword(user, password);

        hash.ShouldNotBe(password);
        hasher.VerifyHashedPassword(user, hash, password)
            .ShouldBeOneOf(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public void Identity_and_actor_ids_should_both_use_guid()
    {
        typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Id))!.PropertyType.ShouldBe(typeof(Guid));
        typeof(ServiceOrderHistoryEntity).GetProperty(nameof(ServiceOrderHistoryEntity.ActorId))!.PropertyType.ShouldBe(typeof(Guid?));
        typeof(ServiceOrderDiagnosisEntity).GetProperty(nameof(ServiceOrderDiagnosisEntity.ActorId))!.PropertyType.ShouldBe(typeof(Guid?));
    }

    [Fact]
    public void Should_define_only_expected_initial_roles() =>
        ApplicationRoles.All.ShouldBe(["PlatformAdmin", "Owner", "Receptionist", "Mechanic"], ignoreOrder: false);

    private static GarageDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GarageDbContext>()
            .UseNpgsql("Host=localhost;Database=identity-model-tests;Username=test;Password=test")
            .Options;
        return new GarageDbContext(options);
    }
}
