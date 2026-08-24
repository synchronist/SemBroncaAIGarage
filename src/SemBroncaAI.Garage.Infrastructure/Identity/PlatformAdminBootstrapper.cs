using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class PlatformAdminBootstrapper(
    GarageDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IConfiguration configuration)
{
    public async Task<PlatformAdminBootstrapResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var normalizedRole = ApplicationRoles.PlatformAdmin.ToUpperInvariant();
        var configured = await (from existingUser in context.Users.AsNoTracking()
                                join userRole in context.UserRoles.AsNoTracking() on existingUser.Id equals userRole.UserId
                                join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                                where role.NormalizedName == normalizedRole
                                select existingUser.Id).AnyAsync(cancellationToken);
        if (configured)
            return new(true, "PlatformAdmin já configurado.");

        var name = configuration["BootstrapAdmin:Name"]?.Trim();
        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        var userName = configuration["BootstrapAdmin:UserName"]?.Trim();
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return new(false, "Configure nome, e-mail, username e senha do bootstrap em configuração segura.");

        if (await userManager.FindByEmailAsync(email) is not null || await userManager.FindByNameAsync(userName) is not null)
            return new(false, "O e-mail ou username informado já pertence a outro usuário.");

        if (!await roleManager.RoleExistsAsync(ApplicationRoles.PlatformAdmin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.PlatformAdmin));
            if (!roleResult.Succeeded) return new(false, "Não foi possível preparar o perfil PlatformAdmin.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var user = ApplicationUser.CreatePlatformAdmin(name, email, userName);
        user.EmailConfirmed = true;
        user.LockoutEnabled = true;
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(false, "A credencial do bootstrap não atende à política de identidade.");
        }
        var roleAssignment = await userManager.AddToRoleAsync(user, ApplicationRoles.PlatformAdmin);
        if (!roleAssignment.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(false, "Não foi possível atribuir o perfil PlatformAdmin.");
        }
        await transaction.CommitAsync(cancellationToken);
        return new(true, "PlatformAdmin configurado com sucesso.");
    }
}

public sealed record PlatformAdminBootstrapResult(bool Succeeded, string Message);
