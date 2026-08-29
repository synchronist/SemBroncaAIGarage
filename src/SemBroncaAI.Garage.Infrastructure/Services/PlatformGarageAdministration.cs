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
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class PlatformGarageAdministration(
    GarageDbContext context,
    UserManager<ApplicationUser> userManager,
    IOptions<SubscriptionOptions> subscriptionOptions,
    IAuditWriter auditWriter,
    ICurrentUser currentUser,
    ITeamInvitationSender invitationSender,
    IConfiguration configuration) : IPlatformGarageAdministration, IPublicGarageSignup
{
    public async Task<PlatformDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var sevenDaysAgo = now.AddDays(-7);
        var fourteenDaysAgo = now.AddDays(-14);
        var thirtyDaysAgo = now.AddDays(-30);
        var total = await context.Garages.AsNoTracking().CountAsync(cancellationToken);
        var active = await context.Garages.AsNoTracking().CountAsync(x => x.Active, cancellationToken);
        var users = await context.Users.AsNoTracking().CountAsync(x => x.GarageId != null, cancellationToken);
        var trials = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Trial, cancellationToken);
        var subscriptions = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Active, cancellationToken);
        var suspended = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Suspended, cancellationToken);
        var pastDue = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.PastDue, cancellationToken);
        var cancelled = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.Status == SubscriptionStatus.Cancelled, cancellationToken);
        var recent = await context.Garages.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Take(5)
            .Select(x => new PlatformRecentGarage(x.Id, x.Name, x.Active, x.CreatedAt))
            .ToArrayAsync(cancellationToken);
        var orderSummary = await context.ServiceOrders.AsNoTracking().GroupBy(x => x.GarageId).Select(group => new
        {
            GarageId = group.Key,
            LastAt = group.Max(x => x.CreatedAt),
            Last30 = group.Count(x => x.CreatedAt >= thirtyDaysAgo)
        }).ToArrayAsync(cancellationToken);
        var historySummary = await context.ServiceOrderHistories.AsNoTracking().GroupBy(x => x.ServiceOrder.GarageId)
            .Select(group => new { GarageId = group.Key, LastAt = group.Max(x => x.CreatedAt) })
            .ToArrayAsync(cancellationToken);
        var subscriptionsByGarage = await (
            from garage in context.Garages.AsNoTracking()
            join subscription in context.GarageSubscriptions.AsNoTracking() on garage.Id equals subscription.GarageId
            select new
            {
                garage.Id, garage.Name, garage.Active, garage.CreatedAt,
                subscription.Status, subscription.Plan, subscription.TrialEndsAt,
                subscription.CurrentPeriodEnd, subscription.PastDueAt, subscription.SuspendedAt
            }).ToArrayAsync(cancellationToken);
        var ordersByGarage = orderSummary.ToDictionary(x => x.GarageId);
        var historyByGarage = historySummary.ToDictionary(x => x.GarageId);
        var garageFacts = subscriptionsByGarage.Select(x => new
        {
            x.Id, x.Name, x.Active, x.CreatedAt, x.Status, x.Plan, x.TrialEndsAt,
            x.CurrentPeriodEnd, x.PastDueAt, x.SuspendedAt,
            LastOrderAt = ordersByGarage.TryGetValue(x.Id, out var order) ? (DateTimeOffset?)order.LastAt : null,
            LastHistoryAt = historyByGarage.TryGetValue(x.Id, out var history) ? (DateTimeOffset?)history.LastAt : null,
            OrdersLast30 = ordersByGarage.TryGetValue(x.Id, out order) ? order.Last30 : 0
        }).ToArray();

        var facts = garageFacts.Select(x => new
        {
            Source = x,
            LastActivity = x.LastOrderAt is null ? x.LastHistoryAt :
                x.LastHistoryAt is null || x.LastOrderAt >= x.LastHistoryAt ? x.LastOrderAt : x.LastHistoryAt
        }).ToArray();
        var nextSevenDays = now.AddDays(7);
        var relevant = facts.Select(x =>
        {
            var (situation, priority) = x.Source.Status switch
            {
                SubscriptionStatus.PastDue => ("Pagamento pendente", 1),
                SubscriptionStatus.Suspended => ("Assinatura suspensa", 2),
                SubscriptionStatus.Trial when x.Source.TrialEndsAt >= now && x.Source.TrialEndsAt <= nextSevenDays => ("Trial termina em breve", 3),
                _ when x.LastActivity is null => ("Ainda sem ordem de serviço", 4),
                _ when x.LastActivity < thirtyDaysAgo => ("Sem atividade há mais de 30 dias", 5),
                _ when x.LastActivity < fourteenDaysAgo => ("Sem atividade há 14 dias", 6),
                _ when x.LastActivity < sevenDaysAgo => ("Sem atividade há 7 dias", 7),
                _ => ("Operação recente", 8)
            };
            var billingReference = x.Source.Status == SubscriptionStatus.Trial
                ? x.Source.TrialEndsAt
                : x.Source.CurrentPeriodEnd ?? x.Source.PastDueAt ?? x.Source.SuspendedAt;
            return new PlatformRelevantGarage(x.Source.Id, x.Source.Name, x.Source.Active, x.Source.Status,
                x.Source.Plan, billingReference, x.LastActivity, x.Source.OrdersLast30, situation, priority);
        }).OrderBy(x => x.RiskPriority).ThenBy(x => x.LastOperationalActivityAt).ThenBy(x => x.Name).Take(12).ToArray();

        var rawVolume = await context.ServiceOrders.AsNoTracking().Where(x => x.CreatedAt >= thirtyDaysAgo)
            .GroupBy(x => x.CreatedAt.Date).Select(x => new { Date = x.Key, Count = x.Count() })
            .ToArrayAsync(cancellationToken);
        var volumeMap = rawVolume.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var volume = Enumerable.Range(0, 30).Select(offset => DateOnly.FromDateTime(today.AddDays(offset - 29)))
            .Select(date => new PlatformDailyVolume(date, volumeMap.GetValueOrDefault(date))).ToArray();

        return new(total, active, total - active, users, trials, subscriptions, suspended, recent)
        {
            PastDueSubscriptions = pastDue,
            CancelledSubscriptions = cancelled,
            NewGaragesLast30Days = facts.Count(x => x.Source.CreatedAt >= thirtyDaysAgo),
            TrialsStartedLast30Days = await context.GarageSubscriptions.AsNoTracking().CountAsync(x => x.StartedAt >= thirtyDaysAgo, cancellationToken),
            ActiveGaragesToday = facts.Count(x => x.LastActivity >= today),
            ActiveGaragesLast7Days = facts.Count(x => x.LastActivity >= sevenDaysAgo),
            ActiveGaragesLast30Days = facts.Count(x => x.LastActivity >= thirtyDaysAgo),
            ServiceOrdersToday = await context.ServiceOrders.AsNoTracking().CountAsync(x => x.CreatedAt >= today, cancellationToken),
            ServiceOrdersLast30Days = await context.ServiceOrders.AsNoTracking().CountAsync(x => x.CreatedAt >= thirtyDaysAgo, cancellationToken),
            DigitalApprovalsLast30Days = await context.ServiceOrderEstimateApprovals.AsNoTracking()
                .CountAsync(x => (x.Status == Domain.Entities.ServiceOrder.EstimateApprovalStatus.Approved ||
                                  x.Status == Domain.Entities.ServiceOrder.EstimateApprovalStatus.PartiallyApproved) &&
                                 x.RespondedAt >= thirtyDaysAgo, cancellationToken),
            DigitalApprovalWaiversLast30Days = await context.ServiceOrders.AsNoTracking()
                .CountAsync(x => x.DigitalApprovalWaivedAt >= thirtyDaysAgo, cancellationToken),
            TrialsEndingNext7Days = facts.Count(x => x.Source.Status == SubscriptionStatus.Trial && x.Source.TrialEndsAt >= now && x.Source.TrialEndsAt <= nextSevenDays),
            GaragesWithoutServiceOrders = facts.Count(x => x.LastActivity is null),
            Inactive7Days = facts.Count(x => x.LastActivity >= fourteenDaysAgo && x.LastActivity < sevenDaysAgo),
            Inactive14Days = facts.Count(x => x.LastActivity >= thirtyDaysAgo && x.LastActivity < fourteenDaysAgo),
            Inactive30Days = facts.Count(x => x.LastActivity is not null && x.LastActivity < thirtyDaysAgo),
            RelevantGarages = relevant,
            ServiceOrderVolume = volume
        };
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
        => await CreateCoreAsync(command, currentUser.UserId, "PlatformAdmin", cancellationToken);

    public async Task<CreatePlatformGarageResponse> SignupAsync(
        PublicGarageSignupCommand command, CancellationToken cancellationToken = default)
    {
        var signupErrors = new Dictionary<string, string[]>();
        var normalizedDocument = BrazilianDocument.Normalize(command.Document);
        if (!BrazilianDocument.IsValid(normalizedDocument))
            signupErrors["document"] = ["Informe um CPF ou CNPJ válido."];
        if (!BrazilianPhone.IsValid(command.Phone))
            signupErrors["phone"] = ["Informe um telefone válido."];
        if (!command.AcceptedTerms)
            signupErrors["acceptedTerms"] = ["É necessário aceitar os Termos de Uso e a Política de Privacidade."];
        if (signupErrors.Count > 0) throw new PlatformGarageValidationException(signupErrors);
        var platformAdminRoleId = await context.Roles.AsNoTracking()
            .Where(role => role.NormalizedName == ApplicationRoles.PlatformAdmin.ToUpperInvariant())
            .Select(role => (Guid?)role.Id).SingleOrDefaultAsync(cancellationToken);
        var systemActorId = platformAdminRoleId is null ? null : await context.UserRoles.AsNoTracking()
            .Where(userRole => userRole.RoleId == platformAdminRoleId)
            .Select(userRole => (Guid?)userRole.UserId).FirstOrDefaultAsync(cancellationToken);
        if (systemActorId is null)
            throw new InvalidOperationException("O cadastro público não está disponível no momento.");
        var platformCommand = new CreatePlatformGarageCommand(command.Name, normalizedDocument,
            BrazilianPhone.Normalize(command.Phone),
            command.Email, command.OwnerName, command.OwnerEmail, command.OwnerEmail);
        return await CreateCoreAsync(platformCommand, systemActorId.Value, "PublicSignup", cancellationToken);
    }

    private async Task<CreatePlatformGarageResponse> CreateCoreAsync(
        CreatePlatformGarageCommand command, Guid actorUserId, string actorContext,
        CancellationToken cancellationToken)
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
            var invitation = new TeamInvitationEntity(garage.Id, owner.Id, actorUserId,
                InvitationTokens.Hash(token), DateTime.UtcNow.AddHours(24));
            context.TeamInvitations.Add(invitation);

            AddOnboardingAudit(garage.Id, actorUserId, actorContext, AuditActions.GarageCreated,
                "Garage", garage.Id, "Oficina criada com assinatura inicial.");
            AddOnboardingAudit(garage.Id, actorUserId, actorContext, AuditActions.OwnerPendingCreated,
                "ApplicationUser", owner.Id, "Proprietário criado aguardando ativação.");
            AddOnboardingAudit(garage.Id, actorUserId, actorContext, AuditActions.OwnerInvitationCreated,
                "TeamInvitation", invitation.Id, "Convite inicial do proprietário gerado.");
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (garage, owner, invitation, token);
        });

        var deliveryStatus = await DeliverOwnerInvitationAsync(
            result.garage.Name, result.owner, result.invitation, result.token, cancellationToken);
        return new(result.garage.Id, result.garage.Name, result.garage.Active, deliveryStatus);
    }

    private void AddOnboardingAudit(Guid garageId, Guid actorUserId, string actorContext,
        string action, string entityType, Guid entityId, string summary)
    {
        if (actorContext == "PlatformAdmin")
        {
            auditWriter.Add(garageId, action, entityType, entityId.ToString("D"), summary);
            return;
        }
        context.AuditEntries.Add(new AuditEntryEntity(DateTime.UtcNow, actorUserId, actorContext,
            garageId, action, entityType, entityId.ToString("D"), summary));
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
