using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.TeamManagement;
using SemBroncaAI.Garage.Domain.Entities;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class TeamManagement(
    GarageDbContext context, UserManager<ApplicationUser> userManager,
    ICurrentGarage currentGarage, ICurrentUser currentUser,
    ITeamInvitationSender sender, IConfiguration configuration) : ITeamManagement
{
    private static readonly string[] MemberRoles = [ApplicationRoles.Receptionist, ApplicationRoles.Mechanic];

    public async Task<IReadOnlyCollection<TeamMemberItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        var garageId = currentGarage.RequireGarageId();
        var query = context.Users.AsNoTracking().Where(x => x.GarageId == garageId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, term) || EF.Functions.ILike(x.Email!, term) || EF.Functions.ILike(x.UserName!, term));
        }
        var users = await query.OrderBy(x => x.Name).ToArrayAsync(cancellationToken);
        var result = new List<TeamMemberItem>(users.Length);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(x => x is ApplicationRoles.Owner or ApplicationRoles.Receptionist or ApplicationRoles.Mechanic);
            if (role is not null) result.Add(new(user.Id, user.Name, user.Email ?? "", user.UserName ?? "", role, user.Active));
        }
        return result;
    }

    public async Task<TeamMemberDetails?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var garageId = currentGarage.RequireGarageId();
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == id && x.GarageId == garageId, cancellationToken);
        if (user is null) return null;
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault(x => x is ApplicationRoles.Owner or ApplicationRoles.Receptionist or ApplicationRoles.Mechanic);
        return role is null ? null : new(user.Id, user.Name, user.Email ?? "", user.UserName ?? "", role, user.Active, user.Id == currentUser.UserId);
    }

    public async Task<TeamOperationResult> InviteAsync(InviteTeamMemberCommand command, CancellationToken cancellationToken = default)
    {
        var garageId = currentGarage.RequireGarageId();
        var errors = ValidateInvite(command);
        if (errors.Count > 0) return new(false, "validation", errors);
        if (await userManager.FindByEmailAsync(command.Email.Trim()) is not null) return Conflict("email", "Este e-mail já está em uso.");
        if (await userManager.FindByNameAsync(command.UserName.Trim()) is not null) return Conflict("userName", "Este nome de usuário já está em uso.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var user = ApplicationUser.CreateGarageUser(command.Name, command.Email, command.UserName, garageId);
        user.Deactivate(); user.EmailConfirmed = false; user.LockoutEnabled = true;
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded) return new(false, "invalid", new Dictionary<string, string[]> { ["form"] = ["Não foi possível preparar o convite."] });
        if (!(await userManager.AddToRoleAsync(user, command.Role)).Succeeded) return new(false, "invalid");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitation = new TeamInvitationEntity(garageId, user.Id, currentUser.UserId, Hash(token), DateTime.UtcNow.AddHours(24));
        context.TeamInvitations.Add(invitation);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var baseUrl = configuration["Web:BaseUrl"] ?? throw new InvalidOperationException("Web:BaseUrl não configurada.");
        var link = $"{baseUrl.TrimEnd('/')}/accept-invitation?id={invitation.Id:D}&token={Uri.EscapeDataString(token)}";
        await sender.SendAsync(user.Email!, link, cancellationToken);
        return new(true);
    }

    public async Task<TeamOperationResult> UpdateAsync(Guid id, UpdateTeamMemberCommand command, CancellationToken cancellationToken = default)
    {
        var garageId = currentGarage.RequireGarageId();
        if (id == currentUser.UserId) return new(false, "self-protected");
        if (!MemberRoles.Contains(command.Role, StringComparer.Ordinal)) return new(false, "validation");
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == id && x.GarageId == garageId, cancellationToken);
        if (user is null) return new(false, "not-found");
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(ApplicationRoles.Owner) || roles.Contains(ApplicationRoles.PlatformAdmin)) return new(false, "protected");
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 256) return new(false, "validation", new Dictionary<string, string[]> { ["name"] = ["Informe um nome válido."] });
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        user.Name = command.Name.Trim(); if (command.Active) user.Activate(); else user.Deactivate();
        var oldRoles = roles.Where(MemberRoles.Contains).ToArray();
        if (oldRoles.Length > 0 && !(await userManager.RemoveFromRolesAsync(user, oldRoles)).Succeeded) return new(false, "invalid");
        if (!(await userManager.AddToRoleAsync(user, command.Role)).Succeeded) return new(false, "invalid");
        await userManager.UpdateAsync(user); await userManager.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync(cancellationToken);
        return new(true);
    }

    public async Task<TeamOperationResult> AcceptAsync(AcceptTeamInvitationCommand command, CancellationToken cancellationToken = default)
    {
        var invitation = await context.TeamInvitations.SingleOrDefaultAsync(x => x.Id == command.InvitationId, cancellationToken);
        if (invitation is null || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(invitation.TokenHash), Convert.FromHexString(Hash(command.Token)))) return new(false, "invalid");
        if (invitation.UsedAt is not null) return new(false, "used");
        if (invitation.ExpiresAt <= DateTime.UtcNow) return new(false, "expired");
        if (command.Password != command.ConfirmPassword) return new(false, "mismatch");
        var user = await userManager.FindByIdAsync(invitation.UserId.ToString());
        if (user is null || user.GarageId != invitation.GarageId || user.Active) return new(false, "invalid");
        var result = await userManager.AddPasswordAsync(user, command.Password);
        if (!result.Succeeded) return new(false, "password", new Dictionary<string, string[]> { ["password"] = PasswordMessages(result.Errors) });
        user.EmailConfirmed = true; user.Activate(); invitation.MarkUsed(DateTime.UtcNow);
        await userManager.UpdateAsync(user); await userManager.UpdateSecurityStampAsync(user); await context.SaveChangesAsync(cancellationToken);
        return new(true);
    }

    private static Dictionary<string, string[]> ValidateInvite(InviteTeamMemberCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors["name"] = ["Informe o nome do membro."];
        if (!System.Net.Mail.MailAddress.TryCreate(command.Email?.Trim(), out _)) errors["email"] = ["Informe um e-mail válido."];
        if (string.IsNullOrWhiteSpace(command.UserName) || command.UserName.Trim().Length > 100) errors["userName"] = ["Informe um nome de usuário válido."];
        if (!MemberRoles.Contains(command.Role, StringComparer.Ordinal)) errors["role"] = ["Selecione um perfil válido."];
        return errors;
    }
    private static TeamOperationResult Conflict(string field, string message) => new(false, "conflict", new Dictionary<string, string[]> { [field] = [message] });
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    private static string[] PasswordMessages(IEnumerable<IdentityError> errors) => errors.Select(x => x.Code switch { "PasswordTooShort" => "A senha deve ter pelo menos 10 caracteres.", "PasswordRequiresUpper" => "Adicione uma letra maiúscula.", "PasswordRequiresLower" => "Adicione uma letra minúscula.", "PasswordRequiresDigit" => "Adicione um número.", "PasswordRequiresUniqueChars" => "Use quatro caracteres diferentes.", _ => null }).Where(x => x is not null).Cast<string>().ToArray();
}
