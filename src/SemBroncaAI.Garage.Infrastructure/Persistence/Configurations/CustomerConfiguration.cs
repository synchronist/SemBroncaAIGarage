using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration
    : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .HasMaxLength(FieldLengthLimits.PersonName)
            .IsRequired();

        builder.Property(customer => customer.Document)
            .HasMaxLength(FieldLengthLimits.Document)
            .IsRequired();

        builder.Property(customer => customer.Phone)
            .HasMaxLength(FieldLengthLimits.Phone)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(FieldLengthLimits.Email);

        builder.Property(customer => customer.Active)
            .IsRequired();

        builder.Property(customer => customer.CreatedAt)
            .IsRequired();

        builder.HasIndex(customer => new
        {
            customer.GarageId,
            customer.Document
        })
        .IsUnique();

        builder.HasOne(customer => customer.Garage)
            .WithMany()
            .HasForeignKey(customer => customer.GarageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
