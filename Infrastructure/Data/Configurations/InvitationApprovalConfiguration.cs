using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for InvitationApproval entity
/// </summary>
public class InvitationApprovalConfiguration : IEntityTypeConfiguration<InvitationApproval>
{
    public void Configure(EntityTypeBuilder<InvitationApproval> builder)
    {
        builder.ToTable("InvitationApprovals");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityColumn();

        // Configure required properties
        builder.Property(a => a.InvitationId)
            .IsRequired();

        builder.Property(a => a.ApproverId)
            .IsRequired();

        builder.Property(a => a.StepOrder)
            .IsRequired();

        builder.Property(a => a.Decision)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.IsRequired)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure optional properties
        builder.Property(a => a.Comments)
            .HasMaxLength(500);

        // Configure relationships
        builder.HasOne(a => a.Invitation)
            .WithMany(i => i.Approvals)
            .HasForeignKey(a => a.InvitationId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(a => a.EscalatedToUser)
            .WithMany()
            .HasForeignKey(a => a.EscalatedToUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Configure indexes
        builder.HasIndex(a => new { a.InvitationId, a.StepOrder })
            .IsUnique();
        builder.HasIndex(a => a.Decision);
        builder.HasIndex(a => a.ApproverId);
    }
}

