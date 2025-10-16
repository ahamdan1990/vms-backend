using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for ConfigurationAudit entity
/// </summary>
public class ConfigurationAuditConfiguration : IEntityTypeConfiguration<ConfigurationAudit>
{
    public void Configure(EntityTypeBuilder<ConfigurationAudit> builder)
    {
        // Table name
        builder.ToTable("ConfigurationAudits");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.OldValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValue)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.SessionId)
            .HasMaxLength(100);

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(e => e.SystemConfigurationId)
            .HasDatabaseName("IX_ConfigurationAudits_SystemConfigurationId");

        builder.HasIndex(e => new { e.Category, e.Key })
            .HasDatabaseName("IX_ConfigurationAudits_Category_Key");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_ConfigurationAudits_Action");

        builder.HasIndex(e => e.CreatedOn)
            .HasDatabaseName("IX_ConfigurationAudits_CreatedOn");

        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("IX_ConfigurationAudits_CreatedBy");

        builder.HasIndex(e => e.IsAutomated)
            .HasDatabaseName("IX_ConfigurationAudits_IsAutomated");

        builder.HasIndex(e => e.RequiresApproval)
            .HasDatabaseName("IX_ConfigurationAudits_RequiresApproval");

        builder.HasIndex(e => e.IsApproved)
            .HasDatabaseName("IX_ConfigurationAudits_IsApproved");

        // Relationships
        builder.HasOne(e => e.SystemConfiguration)
            .WithMany(c => c.AuditEntries)
            .HasForeignKey(e => e.SystemConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Query filters
        builder.HasQueryFilter(e => e.IsActive);
    }
}
