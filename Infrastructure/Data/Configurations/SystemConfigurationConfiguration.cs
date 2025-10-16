using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for SystemConfiguration entity
/// </summary>
public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        // Table name
        builder.ToTable("SystemConfigurations");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Value)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.DataType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.DefaultValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ValidationRules)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.AllowedValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Group)
            .HasMaxLength(100);

        builder.Property(e => e.Environment)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("All");

        // Indexes
        builder.HasIndex(e => new { e.Category, e.Key })
            .IsUnique()
            .HasDatabaseName("IX_SystemConfigurations_Category_Key");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_SystemConfigurations_Category");

        builder.HasIndex(e => e.Environment)
            .HasDatabaseName("IX_SystemConfigurations_Environment");

        builder.HasIndex(e => e.Group)
            .HasDatabaseName("IX_SystemConfigurations_Group");

        builder.HasIndex(e => e.IsEncrypted)
            .HasDatabaseName("IX_SystemConfigurations_IsEncrypted");

        builder.HasIndex(e => e.RequiresRestart)
            .HasDatabaseName("IX_SystemConfigurations_RequiresRestart");

        // Relationships
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.AuditEntries)
            .WithOne(a => a.SystemConfiguration)
            .HasForeignKey(a => a.SystemConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query filters
        builder.HasQueryFilter(e => e.IsActive);
    }
}
