using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VisitorAccess
/// </summary>
public class VisitorAccessConfiguration : IEntityTypeConfiguration<VisitorAccess>
{
    public void Configure(EntityTypeBuilder<VisitorAccess> builder)
    {
        builder.ToTable("VisitorAccess");

        builder.HasKey(va => va.Id);

        // Properties
        builder.Property(va => va.UserId)
            .IsRequired();

        builder.Property(va => va.VisitorId)
            .IsRequired();

        builder.Property(va => va.AccessType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(va => va.GrantedOn)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(va => va.GrantedBy)
            .IsRequired(false); // Optional - system-granted access has no user

        // Indexes
        builder.HasIndex(va => new { va.UserId, va.VisitorId })
            .IsUnique()
            .HasDatabaseName("IX_VisitorAccess_UserId_VisitorId");

        builder.HasIndex(va => va.VisitorId)
            .HasDatabaseName("IX_VisitorAccess_VisitorId");

        builder.HasIndex(va => va.UserId)
            .HasDatabaseName("IX_VisitorAccess_UserId");

        // Relationships
        builder.HasOne(va => va.User)
            .WithMany()
            .HasForeignKey(va => va.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete users

        builder.HasOne(va => va.Visitor)
            .WithMany(v => v.VisitorAccesses)
            .HasForeignKey(va => va.VisitorId)
            .OnDelete(DeleteBehavior.Cascade); // If visitor is deleted, delete access records

        builder.HasOne(va => va.GrantedByUser)
            .WithMany()
            .HasForeignKey(va => va.GrantedBy)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}
