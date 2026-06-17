using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class LicenseRecordConfiguration : IEntityTypeConfiguration<LicenseRecord>
{
    public void Configure(EntityTypeBuilder<LicenseRecord> builder)
    {
        builder.ToTable("LicenseRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LicenseId).IsRequired().HasMaxLength(36);
        builder.Property(e => e.LicenseType).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.IssuedAt).IsRequired();
        builder.Property(e => e.LastValidatedAt).IsRequired();
        builder.Property(e => e.ComponentScoresJson).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FailureReason).HasMaxLength(500);
        builder.Property(e => e.RevocationReason).HasMaxLength(500);

        builder.HasIndex(e => e.LicenseId)
            .IsUnique()
            .HasDatabaseName("IX_LicenseRecords_LicenseId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_LicenseRecords_Status");

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
