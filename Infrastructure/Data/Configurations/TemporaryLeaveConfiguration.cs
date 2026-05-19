using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class TemporaryLeaveConfiguration : IEntityTypeConfiguration<TemporaryLeave>
{
    public void Configure(EntityTypeBuilder<TemporaryLeave> builder)
    {
        builder.ToTable("TemporaryLeaves");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PersonType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.LeftAt).IsRequired();

        builder.Property(e => e.Reason).HasMaxLength(200);

        builder.HasOne(e => e.StaffPresence)
            .WithMany()
            .HasForeignKey(e => e.StaffPresenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Invitation)
            .WithMany()
            .HasForeignKey(e => e.InvitationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecordedByUser)
            .WithMany()
            .HasForeignKey(e => e.RecordedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReturnedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReturnedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.PersonType, e.IsActive })
            .HasDatabaseName("IX_TemporaryLeaves_PersonType_IsActive");

        builder.HasIndex(e => e.StaffPresenceId)
            .HasDatabaseName("IX_TemporaryLeaves_StaffPresenceId");

        builder.HasIndex(e => e.InvitationId)
            .HasDatabaseName("IX_TemporaryLeaves_InvitationId");
    }
}
