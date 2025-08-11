using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for InvitationTemplate entity
/// </summary>
public class InvitationTemplateConfiguration : IEntityTypeConfiguration<InvitationTemplate>
{
    public void Configure(EntityTypeBuilder<InvitationTemplate> builder)
    {
        builder.ToTable("InvitationTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();

        // Configure required properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.DefaultDurationHours)
            .IsRequired()
            .HasDefaultValue(2.0);

        // Configure optional properties
        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.MessageTemplate)
            .HasMaxLength(1000);

        builder.Property(t => t.DefaultSpecialInstructions)
            .HasMaxLength(500);

        // Configure boolean properties
        builder.Property(t => t.DefaultRequiresApproval)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.DefaultRequiresEscort)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.DefaultRequiresBadge)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.IsShared)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsSystemTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Configure relationships
        builder.HasOne(t => t.DefaultVisitPurpose)
            .WithMany()
            .HasForeignKey(t => t.DefaultVisitPurposeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.DefaultLocation)
            .WithMany()
            .HasForeignKey(t => t.DefaultLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure soft delete
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Configure audit fields
        builder.Property(t => t.CreatedOn)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .IsRequired();

        builder.Property(t => t.ModifiedOn);

        builder.Property(t => t.ModifiedBy);

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedOn);

        builder.Property(t => t.DeletedBy);

        // Configure indexes
        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.IsShared);
        builder.HasIndex(t => t.IsSystemTemplate);
        builder.HasIndex(t => t.UsageCount);
        builder.HasIndex(t => new { t.Category, t.IsShared });
    }
}
