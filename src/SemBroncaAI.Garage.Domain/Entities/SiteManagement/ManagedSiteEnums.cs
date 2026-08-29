namespace SemBroncaAI.Garage.Domain.Entities.SiteManagement;

public enum ManagedSiteStatus { Lead, AwaitingMaterials, Planning, InDevelopment, AwaitingApproval, Adjustments, ReadyToPublish, Published, Paused, Cancelled }
public enum ManagedSiteFinancialStatus { Current, Pending, Overdue, Suspended }
public enum ManagedSiteContractStatus { Draft, AwaitingSignature, Active, Closed }
public enum ManagedSiteResourceStatus { Active, Inactive, Pending }
public enum ManagedSitePeriodicity { OneTime, Monthly, Quarterly, Semiannual, Annual }
public enum ManagedSiteMailboxType { Mailbox, Alias, Group }

