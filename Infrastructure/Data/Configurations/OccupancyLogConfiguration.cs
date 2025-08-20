using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for OccupancyLog
/// </summary>
public class OccupancyLogConfiguration : BaseEntityConfiguration<OccupancyLog>
{
    protected override void ConfigureDerivedEntity(EntityTypeBuilder<OccupancyLog> builder)
    {
        // Table configuration
        builder.ToTable("OccupancyLogs");

        // Properties
        builder.Property(ol => ol.Date)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(ol => ol.CurrentCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ol => ol.MaxCapacity)
            .IsRequired();

        builder.Property(ol => ol.ReservedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ol => ol.LastUpdated)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Computed columns
        builder.Property(ol => ol.AvailableCapacity)
            .HasComputedColumnSql("[MaxCapacity] - [CurrentCount] - [ReservedCount]", stored: false);

        builder.Property(ol => ol.OccupancyPercentage)
            .HasComputedColumnSql("CASE WHEN [MaxCapacity] > 0 " +
                "THEN CAST(([CurrentCount] + [ReservedCount]) * 100.0 / [MaxCapacity] AS DECIMAL(5,2)) " +
                "ELSE 0 END", stored: false);

        // Relationships
        builder.HasOne(ol => ol.TimeSlot)
            .WithMany()
            .HasForeignKey(ol => ol.TimeSlotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ol => ol.Location)
            .WithMany()
            .HasForeignKey(ol => ol.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ol => ol.Date)
            .HasDatabaseName("IX_OccupancyLogs_Date");

        builder.HasIndex(ol => new { ol.Date, ol.LocationId })
            .HasDatabaseName("IX_OccupancyLogs_Date_Location");

        builder.HasIndex(ol => new { ol.Date, ol.TimeSlotId })
            .HasDatabaseName("IX_OccupancyLogs_Date_TimeSlot");

        builder.HasIndex(ol => ol.LastUpdated)
            .HasDatabaseName("IX_OccupancyLogs_LastUpdated");

        // Unique constraint to prevent duplicate records
        builder.HasIndex(ol => new { ol.Date, ol.TimeSlotId, ol.LocationId })
            .IsUnique()
            .HasDatabaseName("UX_OccupancyLogs_Date_TimeSlot_Location");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_OccupancyLogs_CurrentCount", "[CurrentCount] >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_OccupancyLogs_MaxCapacity", "[MaxCapacity] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_OccupancyLogs_ReservedCount", "[ReservedCount] >= 0"));
    }
}
