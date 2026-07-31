using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration
    : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(customer => customer.Document)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(150);

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