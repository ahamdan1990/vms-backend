using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for InvitationEvent entity
/// </summary>
public class InvitationEventConfiguration : IEntityTypeConfiguration<InvitationEvent>
{
    public void Configure(EntityTypeBuilder<InvitationEvent> builder)
    {
        // Configure table
        builder.ToTable("InvitationEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();

        // Configure required properties
        builder.Property(e => e.InvitationId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.EventTimestamp)
            .IsRequired();

        // Configure optional properties
        builder.Property(e => e.EventData)
            .HasColumnType("nvarchar(max)");

        // Configure relationships
        builder.HasOne(e => e.Invitation)
            .WithMany(i => i.Events)
            .HasForeignKey(e => e.InvitationId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(e => e.TriggeredByUser)
            .WithMany()
            .HasForeignKey(e => e.TriggeredBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure audit fields
        builder.Property(e => e.CreatedOn)
            .IsRequired();

        builder.Property(e => e.CreatedBy);
        builder.Property(e => e.ModifiedOn);
        builder.Property(e => e.ModifiedBy);

        // Configure indexes
        builder.HasIndex(e => e.InvitationId);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.EventTimestamp);
        builder.HasIndex(e => e.TriggeredBy);
        builder.HasIndex(e => new { e.InvitationId, e.EventTimestamp });
    }
}
