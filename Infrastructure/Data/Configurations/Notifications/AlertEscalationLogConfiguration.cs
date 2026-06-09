using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations.Notifications;

public class AlertEscalationLogConfiguration : IEntityTypeConfiguration<AlertEscalationLog>
{
    public void Configure(EntityTypeBuilder<AlertEscalationLog> builder)
    {
        builder.ToTable("AlertEscalationLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NotificationAlertId).IsRequired();
        builder.Property(e => e.AlertEscalationId).IsRequired();
        builder.Property(e => e.AttemptNumber).IsRequired();
        builder.Property(e => e.Action).IsRequired().HasConversion<string>();
        builder.Property(e => e.TargetInfo).HasMaxLength(500);
        builder.Property(e => e.CreatedOn).IsRequired();

        // Composite index for the MaxAttempts lookup performed by NotificationDispatcherService
        builder.HasIndex(e => new { e.NotificationAlertId, e.AlertEscalationId })
            .HasDatabaseName("IX_AlertEscalationLogs_AlertId_RuleId");

        // FK: When a NotificationAlert row is hard-deleted, cascade-delete its logs.
        // In practice NotificationAlerts are soft-deleted (IsActive=false), so this rarely triggers.
        builder.HasOne(e => e.NotificationAlert)
            .WithMany()
            .HasForeignKey(e => e.NotificationAlertId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK: Preserve logs even when the originating rule is soft-deleted/removed.
        builder.HasOne(e => e.AlertEscalation)
            .WithMany()
            .HasForeignKey(e => e.AlertEscalationId)
            .OnDelete(DeleteBehavior.Restrict);

        // RowVersion is inherited from BaseEntity; configure as concurrency token.
        builder.Property(e => e.RowVersion).IsRowVersion();

        // No global query filter — this is an append-only audit table.
        // IsActive (inherited from BaseEntity) always stays true; no soft-delete needed.
    }
}
