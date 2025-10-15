using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Camera
/// Defines database schema, constraints, and relationships for camera entities
/// </summary>
public class CameraConfiguration : SoftDeleteEntityConfiguration<Camera>
{
    protected override void ConfigureSoftDeleteEntity(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("Cameras");

        // Basic properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.CameraType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.ConnectionString)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Username)
            .HasMaxLength(100);

        builder.Property(c => c.Password)
            .HasMaxLength(500); // Encrypted password storage

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(CameraStatus.Inactive);

        builder.Property(c => c.EnableFacialRecognition)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.Priority)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(c => c.FailureCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Manufacturer)
            .HasMaxLength(100);

        builder.Property(c => c.Model)
            .HasMaxLength(100);

        builder.Property(c => c.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(c => c.SerialNumber)
            .HasMaxLength(100);

        // JSON configuration storage
        builder.Property(c => c.ConfigurationJson)
            .HasColumnType("nvarchar(max)");

        // Metadata storage
        builder.Property(c => c.Metadata)
            .HasColumnType("nvarchar(max)");

        // Error message storage
        builder.Property(c => c.LastErrorMessage)
            .HasMaxLength(1000);

        // DateTime properties
        builder.Property(c => c.LastHealthCheck)
            .HasColumnType("datetime2");

        builder.Property(c => c.LastOnlineTime)
            .HasColumnType("datetime2");

        // Foreign key relationships
        builder.HasOne(c => c.Location)
            .WithMany()
            .HasForeignKey(c => c.LocationId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

        // Indexes for performance
        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Cameras_Name");

        builder.HasIndex(c => c.CameraType)
            .HasDatabaseName("IX_Cameras_CameraType");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Cameras_Status");

        builder.HasIndex(c => c.LocationId)
            .HasDatabaseName("IX_Cameras_LocationId");

        builder.HasIndex(c => c.Priority)
            .HasDatabaseName("IX_Cameras_Priority");

        builder.HasIndex(c => c.EnableFacialRecognition)
            .HasDatabaseName("IX_Cameras_EnableFacialRecognition");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_Cameras_IsActive");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Cameras_IsDeleted");

        builder.HasIndex(c => c.LastHealthCheck)
            .HasDatabaseName("IX_Cameras_LastHealthCheck");

        builder.HasIndex(c => c.LastOnlineTime)
            .HasDatabaseName("IX_Cameras_LastOnlineTime");

        builder.HasIndex(c => c.FailureCount)
            .HasDatabaseName("IX_Cameras_FailureCount");

        // Composite indexes for common query patterns
        builder.HasIndex(c => new { c.LocationId, c.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0") // Unique constraint only for non-deleted cameras
            .HasDatabaseName("IX_Cameras_LocationId_Name_Unique");

        builder.HasIndex(c => new { c.IsActive, c.IsDeleted, c.Status })
            .HasDatabaseName("IX_Cameras_IsActive_IsDeleted_Status");

        builder.HasIndex(c => new { c.EnableFacialRecognition, c.IsActive, c.IsDeleted })
            .HasDatabaseName("IX_Cameras_EnableFacialRecognition_IsActive_IsDeleted");

        builder.HasIndex(c => new { c.CameraType, c.IsActive, c.IsDeleted })
            .HasDatabaseName("IX_Cameras_CameraType_IsActive_IsDeleted");

        // Performance-optimized index for health monitoring
        builder.HasIndex(c => new { c.IsActive, c.IsDeleted, c.LastHealthCheck, c.Priority })
            .HasDatabaseName("IX_Cameras_HealthMonitoring");

        // Index for operational cameras query
        builder.HasIndex(c => new { c.IsActive, c.IsDeleted, c.Status, c.Priority })
            .HasDatabaseName("IX_Cameras_Operational");

        // Check constraints for data integrity
        builder.HasCheckConstraint("CK_Cameras_Priority", "[Priority] >= 1 AND [Priority] <= 10");
        builder.HasCheckConstraint("CK_Cameras_FailureCount", "[FailureCount] >= 0");

        // Configure value conversions for enums to ensure consistency
        builder.Property(c => c.CameraType)
            .HasConversion(
                type => (int)type,
                value => (CameraType)value);

        builder.Property(c => c.Status)
            .HasConversion(
                status => (int)status,
                value => (CameraStatus)value);
    }
}