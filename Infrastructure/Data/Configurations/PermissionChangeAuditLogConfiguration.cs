using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for PermissionChangeAuditLog
/// </summary>
public class PermissionChangeAuditLogConfiguration : IEntityTypeConfiguration<PermissionChangeAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionChangeAuditLog> builder)
    {
        builder.ToTable("PermissionChangeAuditLogs");

        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.ChangeType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.ChangedBy)
            .IsRequired();

        builder.Property(p => p.ChangedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.Reason)
            .HasMaxLength(1000);

        builder.Property(p => p.IpAddress)
            .HasMaxLength(50);

        builder.Property(p => p.PreviousValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.NewValue)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(p => p.RoleId)
            .HasDatabaseName("IX_PermissionChangeAuditLogs_RoleId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_PermissionChangeAuditLogs_UserId");

        builder.HasIndex(p => p.PermissionId)
            .HasDatabaseName("IX_PermissionChangeAuditLogs_PermissionId");

        builder.HasIndex(p => p.ChangedBy)
            .HasDatabaseName("IX_PermissionChangeAuditLogs_ChangedBy");

        builder.HasIndex(p => p.ChangedAt)
            .HasDatabaseName("IX_PermissionChangeAuditLogs_ChangedAt");

        builder.HasIndex(p => new { p.ChangeType, p.ChangedAt })
            .HasDatabaseName("IX_PermissionChangeAuditLogs_ChangeType_ChangedAt");

        // Relationships
        builder.HasOne(p => p.Role)
            .WithMany()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false); // Make optional to avoid query filter issues

        builder.HasOne(p => p.Permission)
            .WithMany()
            .HasForeignKey(p => p.PermissionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.ChangedByUser)
            .WithMany()
            .HasForeignKey(p => p.ChangedBy)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false); // Make optional to avoid query filter issues
    }
}
