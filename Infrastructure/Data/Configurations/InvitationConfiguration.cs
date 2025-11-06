using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Invitation entity
/// </summary>
public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        // Configure base entity properties
        builder.ToTable("Invitations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseIdentityColumn();

        // Configure required properties
        builder.Property(i => i.InvitationNumber)
            .IsRequired()
            .HasMaxLength(40);
        
        builder.HasIndex(i => i.InvitationNumber)
            .IsUnique();

        builder.Property(i => i.VisitorId)
            .IsRequired();

        builder.Property(i => i.HostId)
            .IsRequired();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Message)
            .HasMaxLength(1000);

        builder.Property(i => i.ScheduledStartTime)
            .IsRequired();

        builder.Property(i => i.ScheduledEndTime)
            .IsRequired();

        builder.Property(i => i.ExpectedVisitorCount)
            .IsRequired()
            .HasDefaultValue(1);

        // Configure optional properties
        builder.Property(i => i.SpecialInstructions)
            .HasMaxLength(500);

        builder.Property(i => i.ParkingInstructions)
            .HasMaxLength(200);

        builder.Property(i => i.QrCode)
            .HasMaxLength(500);

        builder.Property(i => i.ApprovalComments)
            .HasMaxLength(500);

        builder.Property(i => i.RejectionReason)
            .HasMaxLength(500);

        builder.Property(i => i.ExternalId)
            .HasMaxLength(100);

        // Configure boolean properties
        builder.Property(i => i.RequiresApproval)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(i => i.RequiresEscort)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.RequiresBadge)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(i => i.NeedsParking)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure relationships
        builder.HasOne(i => i.Visitor)
            .WithMany()
            .HasForeignKey(i => i.VisitorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Host)
            .WithMany()
            .HasForeignKey(i => i.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.VisitPurpose)
            .WithMany(vp => vp.Invitations)
            .HasForeignKey(i => i.VisitPurposeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Location)
            .WithMany(l => l.Invitations)
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.ApprovedByUser)
            .WithMany()
            .HasForeignKey(i => i.ApprovedBy)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(i => i.RejectedByUser)
            .WithMany()
            .HasForeignKey(i => i.RejectedBy)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(i => i.TimeSlot)
            .WithMany()
            .HasForeignKey(i => i.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Configure collections
        builder.HasMany(i => i.Approvals)
            .WithOne(a => a.Invitation)
            .HasForeignKey(a => a.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Events)
            .WithOne(e => e.Invitation)
            .HasForeignKey(e => e.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure soft delete
        builder.HasQueryFilter(i => !i.IsDeleted);

        // Configure audit fields
        builder.Property(i => i.CreatedOn)
            .IsRequired();

        builder.Property(i => i.CreatedBy)
            .IsRequired();

        builder.Property(i => i.ModifiedOn);

        builder.Property(i => i.ModifiedBy);

        builder.Property(i => i.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.DeletedOn);

        builder.Property(i => i.DeletedBy);

        // Configure indexes
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.HostId);
        builder.HasIndex(i => i.VisitorId);
        builder.HasIndex(i => i.ScheduledStartTime);
        builder.HasIndex(i => i.CreatedOn);
        builder.HasIndex(i => i.TimeSlotId);
        builder.HasIndex(i => new { i.Status, i.ScheduledStartTime });
    }
}
