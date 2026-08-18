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
    Task<bool> VerifyPasswordAsync(ApplicationUser user, string password);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user);
    Task<bool?> GetGarageActiveAsync(Guid garageId, CancellationToken cancellationToken);
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

    public Task<bool> VerifyPasswordAsync(ApplicationUser user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    public Task<IList<string>> GetRolesAsync(ApplicationUser user) =>
        userManager.GetRolesAsync(user);

    public Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user) =>
        signInManager.CreateUserPrincipalAsync(user);

    public Task<bool?> GetGarageActiveAsync(Guid garageId, CancellationToken cancellationToken) =>
        dbContext.Garages.AsNoTracking()
            .Where(garage => garage.Id == garageId)
            .Select(garage => (bool?)garage.Active)
            .SingleOrDefaultAsync(cancellationToken);
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
        {
            if (!passwordResult.IsLockedOut)
                return IdentityLoginResult.Failed;

            return await gateway.VerifyPasswordAsync(user, password)
                ? IdentityLoginResult.LockedOut
                : IdentityLoginResult.Failed;
        }

        var roles = await gateway.GetRolesAsync(user);
        var isPlatformAdmin = roles.Contains(ApplicationRoles.PlatformAdmin, StringComparer.Ordinal);
        if (isPlatformAdmin)
        {
            if (user.GarageId is not null)
                return IdentityLoginResult.Failed;
        }
        else if (user.GarageId is null)
        {
            return IdentityLoginResult.Failed;
        }
        else
        {
            var garageActive = await gateway.GetGarageActiveAsync(user.GarageId.Value, cancellationToken);
            if (garageActive is null)
                return IdentityLoginResult.Failed;
            if (!garageActive.Value)
                return IdentityLoginResult.GarageInactive;
        }

        var principal = await gateway.CreatePrincipalAsync(user);
        return new IdentityLoginResult(true, false, false, principal);
    }
}

public sealed record IdentityLoginResult(bool Succeeded, bool IsLockedOut, bool IsGarageInactive, ClaimsPrincipal? Principal)
{
    public static IdentityLoginResult Failed { get; } = new(false, false, false, null);
    public static IdentityLoginResult LockedOut { get; } = new(false, true, false, null);
    public static IdentityLoginResult GarageInactive { get; } = new(false, false, true, null);
}
