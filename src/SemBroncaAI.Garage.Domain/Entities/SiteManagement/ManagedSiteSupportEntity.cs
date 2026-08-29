namespace SemBroncaAI.Garage.Domain.Entities.SiteManagement;
public sealed class ManagedSiteSupportEntity { private ManagedSiteSupportEntity() { } public ManagedSiteSupportEntity(Guid siteId){Id=Guid.CreateVersion7();SiteId=siteId;} public Guid Id {get;private set;} public Guid SiteId {get;private set;} public DateTime OccurredAt {get;set;} public string Type {get;set;}=""; public string Description {get;set;}=""; public decimal HoursSpent {get;set;} public bool Billable {get;set;} public decimal AdditionalValue {get;set;} public string? Status {get;set;} public string? Notes {get;set;} }

