using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using SemBroncaAI.Garage.Application.Common;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.TeamManagement;
using SemBroncaAI.Garage.Domain.Entities;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class PlatformGarageAdministration(
    GarageDbContext context,
    UserManager<ApplicationUser> userManager,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IAuditWriter auditWriter,
    ICurrentUser currentUser,
    ITeamInvitationSender invitationSender,
    IConfiguration configuration) : IPlatformGarageAdministration
{
    public async Task<PlatformDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var total = await context.Garages.AsNoTracking().CountAsync(cancellationToken);
        var active = await context.Garages.AsNoTracking().CountAsync(x => x.Active, cancellationToken);
        var users = await context.Users.AsNoTracking().CountAsync(x => x.GarageId != null, cancellationToken);
        var trials = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Trial, cancellationToken);
        var subscriptions = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Active, cancellationToken);
        var suspended = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Suspended, cancellationToken);
        var recent = await context.Garages.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Take(5)
            .Select(x => new PlatformRecentGarage(x.Id, x.Name, x.Active, x.CreatedAt))
            .ToArrayAsync(cancellationToken);
        return new(total, active, total - active, users, trials, subscriptions, suspended, recent);
    }

    public async Task<PlatformGarageListResponse> ListAsync(
        ListPlatformGaragesQuery query, CancellationToken cancellationToken = default)
    {
        PaginationRules.Validate(query.Page, query.PageSize);
        var garages = context.Garages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            garages = garages.Where(x => EF.Functions.ILike(x.Name, term) || EF.Functions.ILike(x.Document, term));
        }
        if (query.Active.HasValue)
            garages = garages.Where(x => x.Active == query.Active.Value);

        var ownerRoleId = await context.Roles.AsNoTracking()
            .Where(x => x.NormalizedName == ApplicationRoles.Owner.ToUpperInvariant())
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var total = await garages.CountAsync(cancellationToken);
        var items = await garages.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(garage => new PlatformGarageListItem(
                garage.Id, garage.Name, garage.Document, garage.Email, garage.Phone,
                garage.Active, garage.CreatedAt,
                context.Users.Count(user => user.GarageId == garage.Id),
                (from user in context.Users
                 join userRole in context.UserRoles on user.Id equals userRole.UserId
                 where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId
                 orderby user.Id
                 select user.Name).FirstOrDefault(),
                (from user in context.Users
                 join userRole in context.UserRoles on user.Id equals userRole.UserId
                 where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId
                 orderby user.Id
                 select user.Email).FirstOrDefault(),
                context.GarageSubscriptions.Where(subscription => subscription.GarageId == garage.Id)
                    .Select(subscription => subscription.Status).Single()))
            .ToArrayAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        return new(query.Page, query.PageSize, total, pages, items);
    }

    public async Task<PlatformGarageDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ownerRoleId = await context.Roles.AsNoTracking()
            .Where(x => x.NormalizedName == ApplicationRoles.Owner.ToUpperInvariant())
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var garage = await context.Garages.AsNoTracking().Where(x => x.Id == id)
            .Select(garage => new PlatformGarageDetailsResponse(
                garage.Id, garage.Name, garage.Document, garage.Email, garage.Phone,
                garage.Active, garage.CreatedAt,
                context.Users.Count(user => user.GarageId == garage.Id),
                (from user in context.Users join userRole in context.UserRoles on user.Id equals userRole.UserId
                 where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId orderby user.Id select user.Name).FirstOrDefault(),
                (from user in context.Users join userRole in context.UserRoles on user.Id equals userRole.UserId
                 where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId orderby user.Id select user.Email).FirstOrDefault(),
                (from user in context.Users join userRole in context.UserRoles on user.Id equals userRole.UserId
                 where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId orderby user.Id select user.UserName).FirstOrDefault(),
                (from user in context.Users join userRole in context.UserRoles on user.Id equals userRole.UserId
                 where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId orderby user.Id select (bool?)user.Active).FirstOrDefault(),
                context.TeamInvitations
                    .Where(invitation => invitation.GarageId == garage.Id &&
                        (from user in context.Users join userRole in context.UserRoles on user.Id equals userRole.UserId
                         where user.GarageId == garage.Id && userRole.RoleId == ownerRoleId select user.Id).Contains(invitation.UserId))
                    .OrderByDescending(invitation => invitation.CreatedAt).ThenByDescending(invitation => invitation.Id)
                    .Select(invitation => (InvitationDeliveryStatus?)invitation.DeliveryStatus).FirstOrDefault(),
                context.GarageSubscriptions.Where(x => x.GarageId == garage.Id).Select(x => new PlatformSubscriptionResponse(
                    x.Status, x.Plan, x.StartedAt, x.TrialEndsAt, x.CurrentPeriodStart, x.CurrentPeriodEnd,
                    x.SuspendedAt, x.CancelledAt, x.Status == SubscriptionStatus.Trial && x.TrialEndsAt < DateTime.UtcNow)).Single(),
                Array.Empty<PlatformAuditItem>()))
            .SingleOrDefaultAsync(cancellationToken);
        if (garage is null) return null;
        var recentActivity = await context.AuditEntries.AsNoTracking()
            .Where(x => x.GarageId == id)
            .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Take(10)
            .Select(x => new PlatformAuditItem(x.OccurredAt, x.Action, x.ActorContext, x.Summary))
            .ToArrayAsync(cancellationToken);
        return garage with { RecentActivity = recentActivity };
    }

    public async Task<CreatePlatformGarageResponse> CreateAsync(
        CreatePlatformGarageCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);
        var strategy = context.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            var garage = new GarageEntity(command.Name, command.Document, command.Phone, command.Email);
            if (await context.Garages.AnyAsync(x => x.Document == garage.Document, cancellationToken))
                throw new PlatformGarageConflictException("document", "Já existe uma oficina cadastrada com este documento.");
            if (await userManager.FindByEmailAsync(command.OwnerEmail.Trim()) is not null)
                throw new PlatformGarageConflictException("ownerEmail", "Este e-mail de acesso já está em uso.");
            if (await userManager.FindByNameAsync(command.OwnerUserName.Trim()) is not null)
                throw new PlatformGarageConflictException("ownerUserName", "Este nome de usuário já está em uso.");

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            context.Garages.Add(garage);
            var now = DateTime.UtcNow;
            context.GarageSubscriptions.Add(new GarageSubscriptionEntity(
                garage.Id, SubscriptionPlan.Standard, now, subscriptionOptions.Value.DefaultTrialDays));
            await context.SaveChangesAsync(cancellationToken);

            var owner = ApplicationUser.CreateGarageUser(
                command.OwnerName, command.OwnerEmail, command.OwnerUserName, garage.Id);
            owner.Deactivate();
            owner.EmailConfirmed = false;
            owner.LockoutEnabled = true;
            EnsureUserCreated(await userManager.CreateAsync(owner));
            if (!(await userManager.AddToRoleAsync(owner, ApplicationRoles.Owner)).Succeeded)
                throw new InvalidOperationException("Não foi possível concluir o cadastro da oficina.");

            var token = InvitationTokens.Create();
            var invitation = new TeamInvitationEntity(garage.Id, owner.Id, currentUser.UserId,
                InvitationTokens.Hash(token), DateTime.UtcNow.AddHours(24));
            context.TeamInvitations.Add(invitation);

            auditWriter.Add(garage.Id, AuditActions.GarageCreated, "Garage", garage.Id.ToString("D"),
                "Oficina criada com assinatura inicial.");
            auditWriter.Add(garage.Id, AuditActions.OwnerPendingCreated, "ApplicationUser", owner.Id.ToString("D"),
                "Proprietário criado aguardando ativação.");
            auditWriter.Add(garage.Id, AuditActions.OwnerInvitationCreated, "TeamInvitation", invitation.Id.ToString("D"),
                "Convite inicial do proprietário gerado.");
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (garage, owner, invitation, token);
        });

        var deliveryStatus = await DeliverOwnerInvitationAsync(
            result.garage.Name, result.owner, result.invitation, result.token, cancellationToken);
        return new(result.garage.Id, result.garage.Name, result.garage.Active, deliveryStatus);
    }

    public async Task<OwnerInvitationOperationResponse?> ResendOwnerInvitationAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var ownerRoleId = await context.Roles.Where(x => x.NormalizedName == ApplicationRoles.Owner.ToUpperInvariant())
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        var owner = await (from user in context.Users
                           join userRole in context.UserRoles on user.Id equals userRole.UserId
                           where user.GarageId == id && userRole.RoleId == ownerRoleId
                           orderby user.Id select user).FirstOrDefaultAsync(cancellationToken);
        if (owner is null) return null;
        if (owner.Active || owner.EmailConfirmed)
            throw new PlatformGarageConflictException("owner", "O proprietário já está ativo.");

        var invitations = await context.TeamInvitations
            .Where(x => x.GarageId == id && x.UserId == owner.Id && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToArrayAsync(cancellationToken);
        var invitation = invitations.FirstOrDefault();
        if (invitation is null) return null;
        var now = DateTime.UtcNow;
        foreach (var oldInvitation in invitations.Skip(1)) oldInvitation.Invalidate(now);
        var token = InvitationTokens.Create();
        invitation.Renew(InvitationTokens.Hash(token), now.AddHours(24));
        auditWriter.Add(id, AuditActions.OwnerInvitationResent, "TeamInvitation", invitation.Id.ToString("D"),
            "Convite do proprietário reenviado.");
        await context.SaveChangesAsync(cancellationToken);
        var garageName = await context.Garages.Where(x => x.Id == id).Select(x => x.Name).SingleAsync(cancellationToken);
        var status = await DeliverOwnerInvitationAsync(garageName, owner, invitation, token, cancellationToken);
        return new(status);
    }

    public async Task<bool> SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var garage = await context.Garages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (garage is null) return false;
        var subscription = await context.GarageSubscriptions.SingleOrDefaultAsync(x => x.GarageId == id, cancellationToken);
        if (subscription is null) throw new InvalidOperationException("A oficina não possui assinatura cadastrada.");
        var now = DateTime.UtcNow;
        if (active)
        {
            garage.Activate();
            if (subscription.Status is SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled)
                subscription.ChangeStatus(SubscriptionStatus.Active, now);
        }
        else
        {
            garage.Deactivate();
            subscription.ChangeStatus(SubscriptionStatus.Suspended, now);
        }
        auditWriter.Add(id, active ? AuditActions.GarageActivated : AuditActions.GarageDeactivated,
            "Garage", id.ToString("D"), active ? "Oficina ativada." : "Oficina desativada.");
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PlatformSubscriptionResponse?> UpdateSubscriptionAsync(
        Guid id, UpdateGarageSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Status) || !Enum.IsDefined(command.Plan))
            throw new PlatformGarageValidationException(new Dictionary<string, string[]> { ["subscription"] = ["Plano ou status inválido."] });

        var garage = await context.Garages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        var subscription = await context.GarageSubscriptions.SingleOrDefaultAsync(x => x.GarageId == id, cancellationToken);
        if (garage is null || subscription is null) return null;

        if (!SubscriptionTransitions.IsAllowed(subscription.Status, command.Status))
            throw new PlatformGarageValidationException(new Dictionary<string, string[]>
                { ["status"] = ["Esta alteração não é permitida para a situação atual."] });

        var now = DateTime.UtcNow;
        var previousPlan = subscription.Plan;
        var previousStatus = subscription.Status;
        subscription.ChangePlan(command.Plan, now);
        subscription.ChangeStatus(command.Status, now);

        // Garage.Active remains an administrative control. Billing restrictions are enforced independently.
        auditWriter.Add(id, AuditActions.SubscriptionChanged, "GarageSubscription", subscription.Id.ToString("D"),
            $"Plano {previousPlan} → {command.Plan}; status {previousStatus} → {command.Status}.");
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(subscription, now);
    }

    private static PlatformSubscriptionResponse ToResponse(GarageSubscriptionEntity subscription, DateTime now) =>
        new(subscription.Status, subscription.Plan, subscription.StartedAt, subscription.TrialEndsAt,
            subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.SuspendedAt,
            subscription.CancelledAt, subscription.IsTrialExpired(now));

    private static void EnsureUserCreated(IdentityResult result)
    {
        if (result.Succeeded) return;
        throw new PlatformGarageValidationException(new Dictionary<string, string[]>
        {
            ["owner"] = ["Não foi possível preparar o acesso do proprietário."]
        });
    }

    private static void Validate(CreatePlatformGarageCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors["name"] = ["Informe o nome da oficina."];
        else if (command.Name.Trim().Length > 150) errors["name"] = ["O nome da oficina deve ter no máximo 150 caracteres."];
        if (string.IsNullOrWhiteSpace(command.Document) || command.Document.Trim().Length > 20) errors["document"] = ["Informe um documento válido."];
        var phoneDigits = new string((command.Phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (phoneDigits.Length is not (10 or 11)) errors["phone"] = ["Informe um telefone válido."];
        if (!IsEmail(command.Email, 150)) errors["email"] = ["Informe um e-mail válido."];
        if (string.IsNullOrWhiteSpace(command.OwnerName))
            errors["ownerName"] = ["Informe o nome do proprietário."];
        else if (command.OwnerName.Trim().Length > 256) errors["ownerName"] = ["O nome do proprietário é muito longo."];
        if (!IsEmail(command.OwnerEmail, 256)) errors["ownerEmail"] = ["Informe um e-mail válido."];
        if (string.IsNullOrWhiteSpace(command.OwnerUserName))
            errors["ownerUserName"] = ["Informe um nome de usuário."];
        else if (command.OwnerUserName.Trim().Length > 100) errors["ownerUserName"] = ["O nome de usuário é muito longo."];
        if (errors.Count > 0) throw new PlatformGarageValidationException(errors);
    }

    private static bool IsEmail(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximumLength &&
        System.Net.Mail.MailAddress.TryCreate(value.Trim(), out _);

    private async Task<InvitationDeliveryStatus> DeliverOwnerInvitationAsync(string garageName, ApplicationUser owner,
        TeamInvitationEntity invitation, string token, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["App:PublicBaseUrl"] ?? throw new InvalidOperationException("App:PublicBaseUrl não configurada.");
        var link = $"{baseUrl.TrimEnd('/')}/accept-invitation?id={invitation.Id:D}&token={Uri.EscapeDataString(token)}";
        try
        {
            await invitationSender.SendAsync(new(owner.Email!, garageName, ApplicationRoles.Owner, link, invitation.ExpiresAt), cancellationToken);
            invitation.MarkSent(DateTime.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            invitation.MarkDeliveryFailed(DateTime.UtcNow);
        }
        await context.SaveChangesAsync(cancellationToken);
        return invitation.DeliveryStatus;
    }
}
