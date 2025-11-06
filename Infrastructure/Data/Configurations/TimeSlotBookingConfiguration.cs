using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for TimeSlotBooking
/// </summary>
public class TimeSlotBookingConfiguration : SoftDeleteEntityConfiguration<TimeSlotBooking>
{
    protected override void ConfigureSoftDeleteEntity(EntityTypeBuilder<TimeSlotBooking> builder)
    {
        // Table configuration
        builder.ToTable("TimeSlotBookings");

        // Properties
        builder.Property(b => b.BookingDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(b => b.VisitorCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(b => b.Status)
            .IsRequired();

        builder.Property(b => b.Notes)
            .HasMaxLength(500);

        builder.Property(b => b.BookedOn)
            .IsRequired();

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(b => b.TimeSlot)
            .WithMany()
            .HasForeignKey(b => b.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Invitation)
            .WithOne()
            .HasForeignKey<TimeSlotBooking>(b => b.InvitationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.BookedByUser)
            .WithMany()
            .HasForeignKey(b => b.BookedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(b => b.CancelledByUser)
            .WithMany()
            .HasForeignKey(b => b.CancelledBy)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(b => b.TimeSlotId)
            .HasDatabaseName("IX_TimeSlotBookings_TimeSlotId");

        builder.HasIndex(b => b.BookingDate)
            .HasDatabaseName("IX_TimeSlotBookings_BookingDate");

        builder.HasIndex(b => b.InvitationId)
            .HasDatabaseName("IX_TimeSlotBookings_InvitationId");

        builder.HasIndex(b => new { b.TimeSlotId, b.BookingDate, b.Status })
            .HasDatabaseName("IX_TimeSlotBookings_Availability");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_TimeSlotBookings_Status");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_TimeSlotBookings_VisitorCount", "[VisitorCount] > 0"));
    }
}
