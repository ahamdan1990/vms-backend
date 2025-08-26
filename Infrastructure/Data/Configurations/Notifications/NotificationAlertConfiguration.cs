using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations.Notifications;

/// <summary>
/// Entity configuration for NotificationAlert
/// </summary>
public class NotificationAlertConfiguration : IEntityTypeConfiguration<NotificationAlert>
{
    public void Configure(EntityTypeBuilder<NotificationAlert> builder)
    {
        builder.ToTable("NotificationAlerts");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.TargetRole)
            .HasMaxLength(50);

        builder.Property(e => e.RelatedEntityType)
            .HasMaxLength(50);

        builder.Property(e => e.PayloadData)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_NotificationAlerts_Type");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("IX_NotificationAlerts_Priority");

        builder.HasIndex(e => e.TargetUserId)
            .HasDatabaseName("IX_NotificationAlerts_TargetUserId");

        builder.HasIndex(e => e.TargetRole)
            .HasDatabaseName("IX_NotificationAlerts_TargetRole");

        builder.HasIndex(e => e.IsAcknowledged)
            .HasDatabaseName("IX_NotificationAlerts_IsAcknowledged");

        builder.HasIndex(e => new { e.CreatedOn, e.IsAcknowledged })
            .HasDatabaseName("IX_NotificationAlerts_CreatedOn_IsAcknowledged");

        builder.HasIndex(e => e.ExpiresOn)
            .HasDatabaseName("IX_NotificationAlerts_ExpiresOn")
            .HasFilter("[ExpiresOn] IS NOT NULL");

        // Relationships
        builder.HasOne(e => e.TargetUser)
            .WithMany()
            .HasForeignKey(e => e.TargetUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.AcknowledgedByUser)
            .WithMany()
            .HasForeignKey(e => e.AcknowledgedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.TargetLocation)
            .WithMany()
            .HasForeignKey(e => e.TargetLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Audit relationships - use NoAction to avoid cascade conflicts
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
