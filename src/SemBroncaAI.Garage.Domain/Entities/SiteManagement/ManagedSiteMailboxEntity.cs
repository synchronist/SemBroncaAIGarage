namespace SemBroncaAI.Garage.Domain.Entities.SiteManagement;
public sealed class ManagedSiteMailboxEntity { private ManagedSiteMailboxEntity() { } public ManagedSiteMailboxEntity(Guid siteId){Id=Guid.CreateVersion7();SiteId=siteId;} public Guid Id {get;private set;} public Guid SiteId {get;private set;} public string Address {get;set;}=""; public string? OwnerName {get;set;} public ManagedSiteMailboxType Type {get;set;} public ManagedSiteResourceStatus Status {get;set;} public string? Notes {get;set;} }

