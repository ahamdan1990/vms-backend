using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for TimeSlot
/// </summary>
public class TimeSlotConfiguration : SoftDeleteEntityConfiguration<TimeSlot>
{
    protected override void ConfigureSoftDeleteEntity(EntityTypeBuilder<TimeSlot> builder)
    {
        // Table configuration
        builder.ToTable("TimeSlots");

        // Properties
        builder.Property(ts => ts.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ts => ts.StartTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(ts => ts.EndTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(ts => ts.MaxVisitors)
            .IsRequired()
            .HasDefaultValue(50);

        builder.Property(ts => ts.ActiveDays)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("1,2,3,4,5"); // Monday to Friday

        builder.Property(ts => ts.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(ts => ts.BufferMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(ts => ts.AllowOverlapping)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(ts => ts.Location)
            .WithMany()
            .HasForeignKey(ts => ts.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ts => ts.Name)
            .HasDatabaseName("IX_TimeSlots_Name");

        builder.HasIndex(ts => new { ts.LocationId, ts.StartTime, ts.EndTime })
            .HasDatabaseName("IX_TimeSlots_Location_Time");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_TimeSlots_MaxVisitors", "[MaxVisitors] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_TimeSlots_BufferMinutes", "[BufferMinutes] >= 0"));
    }
}
