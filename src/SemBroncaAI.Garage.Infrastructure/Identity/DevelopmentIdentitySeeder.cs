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

        var user = await store.FindUserAsync(options.OwnerEmail, options.OwnerUserName, cancellationToken);
        if (user is null)
        {
            user = await store.CreateOwnerAsync(options, cancellationToken);
        }
        else if (user.GarageId != options.GarageId)
        {
            throw new InvalidOperationException("O usuário configurado para o seed já pertence a outra Garage.");
        }

        if (!user.Active)
            throw new InvalidOperationException("O Owner de Development configurado está inativo.");

        await store.EnsureOwnerRoleAsync(user, cancellationToken);
    }

    private static void Validate(DevelopmentIdentitySeedOptions options)
    {
        if (options.GarageId == Guid.Empty) throw new InvalidOperationException("Configure IdentitySeed:GarageId.");
        if (string.IsNullOrWhiteSpace(options.OwnerName)) throw new InvalidOperationException("Configure IdentitySeed:OwnerName.");
        if (string.IsNullOrWhiteSpace(options.OwnerEmail)) throw new InvalidOperationException("Configure IdentitySeed:OwnerEmail.");
        if (string.IsNullOrWhiteSpace(options.OwnerUserName)) throw new InvalidOperationException("Configure IdentitySeed:OwnerUserName.");
        if (string.IsNullOrWhiteSpace(options.OwnerPassword))
            throw new InvalidOperationException(
                "Configure a senha de Development via user-secrets 'IdentitySeed:OwnerPassword' ou variável 'IdentitySeed__OwnerPassword'.");
    }
}
