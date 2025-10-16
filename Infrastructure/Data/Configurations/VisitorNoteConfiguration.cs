using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VisitorNote
/// </summary>
public class VisitorNoteConfiguration : AuditableEntityConfiguration<VisitorNote>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<VisitorNote> builder)
    {
        builder.ToTable("VisitorNotes");

        // Basic properties
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("General");

        builder.Property(n => n.Priority)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Medium");

        builder.Property(n => n.Tags)
            .HasMaxLength(500);

        // Foreign key
        builder.Property(n => n.VisitorId)
            .IsRequired();

        // Indexes
        builder.HasIndex(n => n.VisitorId)
            .HasDatabaseName("IX_VisitorNotes_VisitorId");

        builder.HasIndex(n => n.Category)
            .HasDatabaseName("IX_VisitorNotes_Category");

        builder.HasIndex(n => n.Priority)
            .HasDatabaseName("IX_VisitorNotes_Priority");

        builder.HasIndex(n => n.IsFlagged)
            .HasDatabaseName("IX_VisitorNotes_IsFlagged");

        builder.HasIndex(n => n.FollowUpDate)
            .HasDatabaseName("IX_VisitorNotes_FollowUpDate");

        // Foreign key relationship
        builder.HasOne(n => n.Visitor)
            .WithMany(v => v.VisitorNotes)
            .HasForeignKey(n => n.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
