namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class DevelopmentIdentitySeeder(IDevelopmentIdentitySeedStore store)
{
    public async Task SeedAsync(DevelopmentIdentitySeedOptions options, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled) return;
        Validate(options);

        if (!await store.GarageExistsAsync(options.GarageId, cancellationToken))
            throw new InvalidOperationException(
                $"A Garage de Development '{options.GarageId}' não existe. O seed não criará outra Garage automaticamente.");

        foreach (var role in ApplicationRoles.All)
            await store.EnsureRoleAsync(role, cancellationToken);

        var users = new[]
        {
            new DevelopmentSeedUser(options.OwnerName, options.OwnerEmail, options.OwnerUserName, options.OwnerPassword, ApplicationRoles.Owner),
            new DevelopmentSeedUser(options.ReceptionistName, options.ReceptionistEmail, options.ReceptionistUserName, options.ReceptionistPassword, ApplicationRoles.Receptionist),
            new DevelopmentSeedUser(options.MechanicName, options.MechanicEmail, options.MechanicUserName, options.MechanicPassword, ApplicationRoles.Mechanic)
        };

        foreach (var user in users)
            await SeedUserAsync(user, options.GarageId, cancellationToken);

        await SeedUserAsync(
            new DevelopmentSeedUser(options.PlatformAdminName, options.PlatformAdminEmail,
                options.PlatformAdminUserName, options.PlatformAdminPassword, ApplicationRoles.PlatformAdmin),
            garageId: null,
            cancellationToken);
    }

    private async Task SeedUserAsync(DevelopmentSeedUser seedUser, Guid? garageId, CancellationToken cancellationToken)
    {
        var user = await store.FindUserAsync(seedUser.Email, seedUser.UserName, cancellationToken);
        if (user is null)
            user = await store.CreateUserAsync(seedUser, garageId, cancellationToken);
        else if (user.GarageId != garageId)
            throw new InvalidOperationException($"O usuário {seedUser.Role} configurado para o seed já pertence a outra Garage.");

        if (!user.Active)
            throw new InvalidOperationException($"O usuário {seedUser.Role} de Development configurado está inativo.");

        await store.EnsureRoleAsync(user, seedUser.Role, cancellationToken);
    }

    private static void Validate(DevelopmentIdentitySeedOptions options)
    {
        if (options.GarageId == Guid.Empty) throw new InvalidOperationException("Configure IdentitySeed:GarageId.");
        ValidateUser(options.OwnerName, options.OwnerEmail, options.OwnerUserName, options.OwnerPassword, "Owner");
        ValidateUser(options.ReceptionistName, options.ReceptionistEmail, options.ReceptionistUserName, options.ReceptionistPassword, "Receptionist");
        ValidateUser(options.MechanicName, options.MechanicEmail, options.MechanicUserName, options.MechanicPassword, "Mechanic");
        ValidateUser(options.PlatformAdminName, options.PlatformAdminEmail, options.PlatformAdminUserName,
            options.PlatformAdminPassword, "PlatformAdmin");
    }

    private static void ValidateUser(string name, string email, string userName, string password, string prefix)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException($"Configure IdentitySeed:{prefix}Name.");
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException($"Configure IdentitySeed:{prefix}Email.");
        if (string.IsNullOrWhiteSpace(userName)) throw new InvalidOperationException($"Configure IdentitySeed:{prefix}UserName.");
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                $"Configure a senha de Development via user-secrets 'IdentitySeed:{prefix}Password' ou variável 'IdentitySeed__{prefix}Password'.");
    }
}
