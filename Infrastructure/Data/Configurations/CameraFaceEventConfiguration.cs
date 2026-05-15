using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class CameraFaceEventConfiguration : IEntityTypeConfiguration<CameraFaceEvent>
{
    public void Configure(EntityTypeBuilder<CameraFaceEvent> builder)
    {
        builder.ToTable("CameraFaceEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.CameraName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.PersonType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.SubjectId)
            .HasMaxLength(200);

        builder.Property(e => e.SnapshotPath)
            .HasMaxLength(500);

        builder.Property(e => e.SuggestedAction)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.ReviewStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(FaceEventReviewStatus.Pending);

        builder.HasOne(e => e.Camera)
            .WithMany()
            .HasForeignKey(e => e.CameraId)
            .OnDelete(DeleteBehavior.Restrict);

        // Querying by camera + time range (live feed panel)
        builder.HasIndex(e => new { e.CameraId, e.CapturedAt })
            .HasDatabaseName("IX_CameraFaceEvents_CameraId_CapturedAt");

        // Review queue: pending events
        builder.HasIndex(e => new { e.ReviewStatus, e.CapturedAt })
            .HasDatabaseName("IX_CameraFaceEvents_ReviewStatus_CapturedAt");

        // Retention job: find expired unknown events
        builder.HasIndex(e => e.ExpiresAt)
            .HasFilter("[ExpiresAt] IS NOT NULL")
            .HasDatabaseName("IX_CameraFaceEvents_ExpiresAt");

        // FIFO candidate count per person
        builder.HasIndex(e => new { e.PersonId, e.PersonType, e.IsKnownCandidate })
            .HasFilter("[PersonId] IS NOT NULL AND [IsKnownCandidate] = 1")
            .HasDatabaseName("IX_CameraFaceEvents_KnownCandidates");

        // EventId lookup from SignalR payload
        builder.HasIndex(e => e.EventId)
            .IsUnique()
            .HasDatabaseName("IX_CameraFaceEvents_EventId");
    }
}
