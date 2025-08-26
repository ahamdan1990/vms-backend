using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations.Notifications;

/// <summary>
/// Entity configuration for AlertEscalation
/// </summary>
public class AlertEscalationConfiguration : IEntityTypeConfiguration<AlertEscalation>
{
    public void Configure(EntityTypeBuilder<AlertEscalation> builder)
    {
        builder.ToTable("AlertEscalations");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.RuleName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AlertType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.AlertPriority)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.TargetRole)
            .HasMaxLength(50);

        builder.Property(e => e.EscalationDelayMinutes)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.EscalationTargetRole)
            .HasMaxLength(50);

        builder.Property(e => e.EscalationEmails)
            .HasMaxLength(500);

        builder.Property(e => e.EscalationPhones)
            .HasMaxLength(200);

        builder.Property(e => e.MaxAttempts)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(e => e.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.RulePriority)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(e => e.Configuration)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(e => e.AlertType)
            .HasDatabaseName("IX_AlertEscalations_AlertType");

        builder.HasIndex(e => e.AlertPriority)
            .HasDatabaseName("IX_AlertEscalations_AlertPriority");

        builder.HasIndex(e => e.TargetRole)
            .HasDatabaseName("IX_AlertEscalations_TargetRole");

        builder.HasIndex(e => e.LocationId)
            .HasDatabaseName("IX_AlertEscalations_LocationId");

        builder.HasIndex(e => e.IsEnabled)
            .HasDatabaseName("IX_AlertEscalations_IsEnabled");

        builder.HasIndex(e => e.RulePriority)
            .HasDatabaseName("IX_AlertEscalations_RulePriority");

        builder.HasIndex(e => new { e.AlertType, e.AlertPriority, e.IsEnabled })
            .HasDatabaseName("IX_AlertEscalations_Type_Priority_Enabled");

        // Relationships
        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // All User relationships use NoAction to avoid cascade conflicts
        builder.HasOne(e => e.EscalationTargetUser)
            .WithMany()
            .HasForeignKey(e => e.EscalationTargetUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.ModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // Auditable fields
        builder.Property(e => e.CreatedOn)
            .IsRequired();

        builder.Property(e => e.ModifiedOn);

        // Soft delete
        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasQueryFilter(e => e.IsActive);

        // Row version for concurrency
        builder.Property(e => e.RowVersion)
            .IsRowVersion();
    }
}
