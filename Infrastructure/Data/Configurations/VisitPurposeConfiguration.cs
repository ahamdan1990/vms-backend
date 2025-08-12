using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VisitPurpose
/// </summary>
public class VisitPurposeConfiguration : AuditableEntityConfiguration<VisitPurpose>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<VisitPurpose> builder)
    {
        builder.ToTable("VisitPurposes");

        // Basic properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.IconName)
            .HasMaxLength(50);

        builder.Property(p => p.ColorCode)
            .HasMaxLength(7); // For hex colors like #FF0000

        builder.Property(p => p.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(p => p.RequiresApproval)
            .HasDefaultValue(false);

        builder.Property(p => p.MaxDurationHours)
            .HasDefaultValue(8);

        // Relationship with Invitations (reverse side)
        builder.HasMany(p => p.Invitations)
            .WithOne(i => i.VisitPurpose)
            .HasForeignKey(i => i.VisitPurposeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.Code)
            .HasDatabaseName("IX_VisitPurposes_Code")
            .IsUnique();

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_VisitPurposes_Name");

        builder.HasIndex(p => p.DisplayOrder)
            .HasDatabaseName("IX_VisitPurposes_DisplayOrder");
    }
}
