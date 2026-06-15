using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class StaffPresenceConfiguration : IEntityTypeConfiguration<StaffPresence>
{
    public void Configure(EntityTypeBuilder<StaffPresence> builder)
    {
        builder.ToTable("StaffPresences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.UserDisplayName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.UserDepartment)
            .HasMaxLength(200);

        builder.Property(e => e.UserJobTitle)
            .HasMaxLength(200);

        builder.Property(e => e.CheckedInAt)
            .IsRequired();

        // FK to User (the staff member)
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserReferenceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Location (optional)
        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK to User who checked them in
        builder.HasOne(e => e.CheckedInByUser)
            .WithMany()
            .HasForeignKey(e => e.CheckedInByReferenceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to User who checked them out (optional)
        builder.HasOne(e => e.CheckedOutByUser)
            .WithMany()
            .HasForeignKey(e => e.CheckedOutById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.UserId, e.Status })
            .HasDatabaseName("IX_StaffPresences_UserId_Status");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_StaffPresences_Status");

        builder.HasIndex(e => e.CheckedInAt)
            .HasDatabaseName("IX_StaffPresences_CheckedInAt");
    }
}
