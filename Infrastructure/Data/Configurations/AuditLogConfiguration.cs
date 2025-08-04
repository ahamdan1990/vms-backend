using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AuditLog
/// </summary>
public class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    protected override void ConfigureDerivedEntity(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // Event properties
        builder.Property(al => al.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(al => al.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.Description)
            .HasMaxLength(500);

        // Data properties
        builder.Property(al => al.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Metadata)
            .HasMaxLength(-1) // -1 = unlimited
            .IsRequired(false);

        // Request properties
        builder.Property(al => al.IpAddress)
            .HasMaxLength(45);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.CorrelationId)
            .HasMaxLength(50);

        builder.Property(al => al.SessionId)
            .HasMaxLength(50);

        builder.Property(al => al.RequestId)
            .HasMaxLength(50);

        builder.Property(al => al.HttpMethod)
            .HasMaxLength(10);

        builder.Property(al => al.RequestPath)
            .HasMaxLength(500);

        // Status properties
        builder.Property(al => al.IsSuccess)
            .HasDefaultValue(true);

        builder.Property(al => al.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(al => al.ExceptionDetails)
            .HasColumnType("nvarchar(max)");

        // Risk and review properties
        builder.Property(al => al.RiskLevel)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Low");

        builder.Property(al => al.RequiresAttention)
            .HasDefaultValue(false);

        builder.Property(al => al.IsReviewed)
            .HasDefaultValue(false);

        builder.Property(al => al.ReviewComments)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(al => al.User)
            .WithMany(u => u.CreatedAuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        builder.HasIndex(al => al.EventType)
            .HasDatabaseName("IX_AuditLogs_EventType");

        builder.HasIndex(al => al.EntityName)
            .HasDatabaseName("IX_AuditLogs_EntityName");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(al => al.IsSuccess)
            .HasDatabaseName("IX_AuditLogs_IsSuccess");

        builder.HasIndex(al => al.RiskLevel)
            .HasDatabaseName("IX_AuditLogs_RiskLevel");

        builder.HasIndex(al => al.RequiresAttention)
            .HasDatabaseName("IX_AuditLogs_RequiresAttention");

        builder.HasIndex(al => al.IsReviewed)
            .HasDatabaseName("IX_AuditLogs_IsReviewed");

        builder.HasIndex(al => al.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");

        builder.HasIndex(al => al.IpAddress)
            .HasDatabaseName("IX_AuditLogs_IpAddress");

        // Composite indexes for common queries
        builder.HasIndex(al => new { al.EventType, al.CreatedOn })
            .HasDatabaseName("IX_AuditLogs_EventType_CreatedOn");

        builder.HasIndex(al => new { al.UserId, al.CreatedOn })
            .HasDatabaseName("IX_AuditLogs_UserId_CreatedOn");

        builder.HasIndex(al => new { al.EntityName, al.EntityId, al.CreatedOn })
            .HasDatabaseName("IX_AuditLogs_Entity_CreatedOn");

        builder.HasIndex(al => new { al.RequiresAttention, al.IsReviewed, al.CreatedOn })
            .HasDatabaseName("IX_AuditLogs_Attention_Review_CreatedOn");

        builder.HasIndex(al => new { al.IsSuccess, al.RiskLevel, al.CreatedOn })
            .HasDatabaseName("IX_AuditLogs_Success_Risk_CreatedOn");
    }
}