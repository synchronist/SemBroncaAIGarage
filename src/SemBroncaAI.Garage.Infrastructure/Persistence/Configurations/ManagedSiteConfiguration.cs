using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.SiteManagement;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ManagedSiteConfiguration : IEntityTypeConfiguration<ManagedSiteEntity>
{
    public void Configure(EntityTypeBuilder<ManagedSiteEntity> b)
    {
        b.ToTable("ManagedSites"); b.HasKey(x=>x.Id); b.HasIndex(x=>x.Domain).IsUnique(); b.HasIndex(x=>new{x.Active,x.Status});
        foreach(var name in new[]{nameof(ManagedSiteEntity.TradeName),nameof(ManagedSiteEntity.ProjectName),nameof(ManagedSiteEntity.Domain)}) b.Property(name).HasMaxLength(200).IsRequired();
        foreach(var name in new[]{nameof(ManagedSiteEntity.LegalName),nameof(ManagedSiteEntity.ContactName),nameof(ManagedSiteEntity.Email),nameof(ManagedSiteEntity.DomainRegistrar),nameof(ManagedSiteEntity.DomainHolder),nameof(ManagedSiteEntity.DnsProvider),nameof(ManagedSiteEntity.HostingProvider),nameof(ManagedSiteEntity.HostingPlan),nameof(ManagedSiteEntity.DeployPlatform),nameof(ManagedSiteEntity.ProductionBranch),nameof(ManagedSiteEntity.EmailProvider),nameof(ManagedSiteEntity.EmailPlan),nameof(ManagedSiteEntity.PaymentMethod),nameof(ManagedSiteEntity.PaymentTerms),nameof(ManagedSiteEntity.DevelopmentPaymentStatus),nameof(ManagedSiteEntity.SiteType)}) b.Property(name).HasMaxLength(200);
        foreach(var name in new[]{nameof(ManagedSiteEntity.Phone),nameof(ManagedSiteEntity.WhatsApp),nameof(ManagedSiteEntity.Document)}) b.Property(name).HasMaxLength(20);
        foreach(var name in new[]{nameof(ManagedSiteEntity.HostingAdminUrl),nameof(ManagedSiteEntity.ProductionUrl),nameof(ManagedSiteEntity.StagingUrl),nameof(ManagedSiteEntity.RepositoryUrl),nameof(ManagedSiteEntity.CredentialReference)}) b.Property(name).HasMaxLength(500);
        foreach(var name in new[]{nameof(ManagedSiteEntity.DomainCost),nameof(ManagedSiteEntity.HostingCost),nameof(ManagedSiteEntity.EmailCost),nameof(ManagedSiteEntity.DevelopmentContractValue),nameof(ManagedSiteEntity.DevelopmentReceivedValue),nameof(ManagedSiteEntity.MonthlyFee),nameof(ManagedSiteEntity.EstimatedRecurringCost),nameof(ManagedSiteEntity.MonthlySupportHours)}) b.Property(name).HasPrecision(14,2);
        b.HasMany(x=>x.Mailboxes).WithOne().HasForeignKey(x=>x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x=>x.Costs).WithOne().HasForeignKey(x=>x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x=>x.SupportEntries).WithOne().HasForeignKey(x=>x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x=>x.History).WithOne().HasForeignKey(x=>x.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ManagedSiteMailboxConfiguration:IEntityTypeConfiguration<ManagedSiteMailboxEntity>{public void Configure(EntityTypeBuilder<ManagedSiteMailboxEntity>b){b.ToTable("ManagedSiteMailboxes");b.HasKey(x=>x.Id);b.Property(x=>x.Address).HasMaxLength(320).IsRequired();b.Property(x=>x.OwnerName).HasMaxLength(200);b.Property(x=>x.Notes).HasMaxLength(2000);}}
public sealed class ManagedSiteCostConfiguration:IEntityTypeConfiguration<ManagedSiteCostEntity>{public void Configure(EntityTypeBuilder<ManagedSiteCostEntity>b){b.ToTable("ManagedSiteCosts");b.HasKey(x=>x.Id);b.Property(x=>x.Description).HasMaxLength(200).IsRequired();b.Property(x=>x.Category).HasMaxLength(80).IsRequired();b.Property(x=>x.Supplier).HasMaxLength(200);b.Property(x=>x.Value).HasPrecision(14,2);b.Property(x=>x.Notes).HasMaxLength(2000);}}
public sealed class ManagedSiteSupportConfiguration:IEntityTypeConfiguration<ManagedSiteSupportEntity>{public void Configure(EntityTypeBuilder<ManagedSiteSupportEntity>b){b.ToTable("ManagedSiteSupportEntries");b.HasKey(x=>x.Id);b.Property(x=>x.Type).HasMaxLength(80).IsRequired();b.Property(x=>x.Description).HasMaxLength(2000).IsRequired();b.Property(x=>x.HoursSpent).HasPrecision(8,2);b.Property(x=>x.AdditionalValue).HasPrecision(14,2);b.Property(x=>x.Status).HasMaxLength(80);b.Property(x=>x.Notes).HasMaxLength(2000);}}
public sealed class ManagedSiteHistoryConfiguration:IEntityTypeConfiguration<ManagedSiteHistoryEntity>{public void Configure(EntityTypeBuilder<ManagedSiteHistoryEntity>b){b.ToTable("ManagedSiteHistory");b.HasKey(x=>x.Id);b.Property(x=>x.Text).HasMaxLength(2000).IsRequired();b.HasIndex(x=>new{x.SiteId,x.CreatedAt});}}

