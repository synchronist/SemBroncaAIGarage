using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Api.Services;

public interface IIdentityLoginGateway
{
    Task<ApplicationUser?> FindByEmailAsync(string identifier);
    Task<ApplicationUser?> FindByNameAsync(string identifier);
    Task<SignInResult> CheckPasswordAsync(ApplicationUser user, string password);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user);
    Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken);
}

public sealed class IdentityLoginGateway(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    GarageDbContext dbContext) : IIdentityLoginGateway
{
    public Task<ApplicationUser?> FindByEmailAsync(string identifier) =>
        userManager.FindByEmailAsync(identifier);

    public Task<ApplicationUser?> FindByNameAsync(string identifier) =>
        userManager.FindByNameAsync(identifier);

    public Task<SignInResult> CheckPasswordAsync(ApplicationUser user, string password) =>
        signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

    public Task<IList<string>> GetRolesAsync(ApplicationUser user) =>
        userManager.GetRolesAsync(user);

    public Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user) =>
        signInManager.CreateUserPrincipalAsync(user);

    public Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken) =>
        dbContext.Garages.AsNoTracking().AnyAsync(garage => garage.Id == garageId, cancellationToken);
}

public sealed class IdentityLoginService(IIdentityLoginGateway gateway)
{
    public async Task<IdentityLoginResult> AuthenticateAsync(
        string? identifier,
        string? password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password))
            return IdentityLoginResult.Failed;

        var normalizedIdentifier = identifier.Trim();
        var user = await gateway.FindByEmailAsync(normalizedIdentifier)
            ?? await gateway.FindByNameAsync(normalizedIdentifier);
        if (user is null || !user.Active)
            return IdentityLoginResult.Failed;

        var passwordResult = await gateway.CheckPasswordAsync(user, password);
        if (!passwordResult.Succeeded)
            return passwordResult.IsLockedOut ? IdentityLoginResult.LockedOut : IdentityLoginResult.Failed;

        var roles = await gateway.GetRolesAsync(user);
        var isPlatformAdmin = roles.Contains(ApplicationRoles.PlatformAdmin, StringComparer.Ordinal);
        if (isPlatformAdmin)
        {
            if (user.GarageId is not null)
                return IdentityLoginResult.Failed;
        }
        else if (user.GarageId is null ||
            !await gateway.GarageExistsAsync(user.GarageId.Value, cancellationToken))
        {
            return IdentityLoginResult.Failed;
        }

        var principal = await gateway.CreatePrincipalAsync(user);
        return new IdentityLoginResult(true, false, principal);
    }
}

public sealed record IdentityLoginResult(bool Succeeded, bool IsLockedOut, ClaimsPrincipal? Principal)
{
    public static IdentityLoginResult Failed { get; } = new(false, false, null);
    public static IdentityLoginResult LockedOut { get; } = new(false, true, null);
}
