using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations.Notifications;

/// <summary>
/// Entity configuration for OperatorSession
/// </summary>
public class OperatorSessionConfiguration : IEntityTypeConfiguration<OperatorSession>
{
    public void Configure(EntityTypeBuilder<OperatorSession> builder)
    {
        builder.ToTable("OperatorSessions");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.ConnectionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.LastActivity)
            .IsRequired();

        builder.Property(e => e.SessionStart)
            .IsRequired();

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_OperatorSessions_UserId");

        builder.HasIndex(e => e.ConnectionId)
            .IsUnique()
            .HasDatabaseName("IX_OperatorSessions_ConnectionId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_OperatorSessions_Status");

        builder.HasIndex(e => e.LocationId)
            .HasDatabaseName("IX_OperatorSessions_LocationId");

        builder.HasIndex(e => new { e.SessionEnd, e.Status })
            .HasDatabaseName("IX_OperatorSessions_SessionEnd_Status");

        builder.HasIndex(e => e.LastActivity)
            .HasDatabaseName("IX_OperatorSessions_LastActivity");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
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
