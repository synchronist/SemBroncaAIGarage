namespace SemBroncaAI.Garage.Domain.Entities.SiteManagement;

public sealed class ManagedSiteEntity
{
    private ManagedSiteEntity() { }
    public ManagedSiteEntity(string tradeName, string projectName, string domain, DateTime now)
    { Id = Guid.CreateVersion7(); TradeName = tradeName.Trim(); ProjectName = projectName.Trim(); Domain = domain.Trim().ToLowerInvariant(); CreatedAt = UpdatedAt = now; }

    public Guid Id { get; private set; }
    public string TradeName { get; set; } = "";
    public string? LegalName { get; set; }
    public string? Document { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public string ProjectName { get; set; } = "";
    public string Domain { get; set; } = "";
    public string? SiteType { get; set; }
    public string? Scope { get; set; }
    public DateOnly? ContractedOn { get; set; }
    public DateOnly? StartedOn { get; set; }
    public DateOnly? ExpectedDeliveryOn { get; set; }
    public DateOnly? PublishedOn { get; set; }
    public ManagedSiteStatus Status { get; set; }
    public int ContractedRevisions { get; set; }
    public int UsedRevisions { get; set; }
    public bool Active { get; set; } = true;

    public string? DomainRegistrar { get; set; }
    public string? DomainHolder { get; set; }
    public bool DomainManagedByClient { get; set; }
    public DateOnly? DomainRenewalOn { get; set; }
    public decimal DomainCost { get; set; }
    public string? DomainNotes { get; set; }
    public string? DnsProvider { get; set; }
    public string? DnsNotes { get; set; }
    public string? HostingProvider { get; set; }
    public string? HostingPlan { get; set; }
    public string? HostingAdminUrl { get; set; }
    public decimal HostingCost { get; set; }
    public ManagedSitePeriodicity HostingPeriodicity { get; set; }
    public DateOnly? HostingRenewalOn { get; set; }
    public ManagedSiteResourceStatus HostingStatus { get; set; }
    public string? DeployPlatform { get; set; }
    public string? ProductionUrl { get; set; }
    public string? StagingUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? ProductionBranch { get; set; }
    public string? DeployNotes { get; set; }
    public string? CredentialReference { get; set; }
    public ManagedSiteResourceStatus SslStatus { get; set; }
    public DateOnly? SslExpiresOn { get; set; }

    public string? EmailProvider { get; set; }
    public string? EmailPlan { get; set; }
    public int EmailIncludedCount { get; set; }
    public decimal EmailCost { get; set; }
    public ManagedSitePeriodicity EmailPeriodicity { get; set; }
    public DateOnly? EmailRenewalOn { get; set; }
    public ManagedSiteResourceStatus EmailStatus { get; set; }

    public decimal DevelopmentContractValue { get; set; }
    public decimal DevelopmentReceivedValue { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DevelopmentPaymentStatus { get; set; }
    public decimal MonthlyFee { get; set; }
    public decimal EstimatedRecurringCost { get; set; }
    public int DueDay { get; set; }
    public DateOnly? NextChargeOn { get; set; }
    public ManagedSiteFinancialStatus FinancialStatus { get; set; }

    public bool HasContract { get; set; }
    public ManagedSiteContractStatus ContractStatus { get; set; }
    public DateOnly? ContractStartOn { get; set; }
    public DateOnly? ContractSignedOn { get; set; }
    public int ContractTermMonths { get; set; }
    public int CancellationNoticeDays { get; set; }
    public int IncludedRevisions { get; set; }
    public decimal MonthlySupportHours { get; set; }
    public string? ContractNotes { get; set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; set; }
    public List<ManagedSiteMailboxEntity> Mailboxes { get; private set; } = [];
    public List<ManagedSiteCostEntity> Costs { get; private set; } = [];
    public List<ManagedSiteSupportEntity> SupportEntries { get; private set; } = [];
    public List<ManagedSiteHistoryEntity> History { get; private set; } = [];
}

