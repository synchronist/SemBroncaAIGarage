namespace SemBroncaAI.Garage.Domain.Entities.SiteManagement;
public sealed class ManagedSiteCostEntity { private ManagedSiteCostEntity() { } public ManagedSiteCostEntity(Guid siteId){Id=Guid.CreateVersion7();SiteId=siteId;} public Guid Id {get;private set;} public Guid SiteId {get;private set;} public string Description {get;set;}=""; public string Category {get;set;}=""; public string? Supplier {get;set;} public decimal Value {get;set;} public ManagedSitePeriodicity Periodicity {get;set;} public DateOnly? NextRenewalOn {get;set;} public string? Notes {get;set;} }

