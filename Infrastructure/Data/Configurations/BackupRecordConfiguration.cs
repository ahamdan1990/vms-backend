using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.ToTable("BackupRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TriggerType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.TriggeredByDisplay).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.FilePath).HasMaxLength(500);
        builder.Property(e => e.FileName).HasMaxLength(200);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.DatabaseName).HasMaxLength(128);

        builder.HasIndex(e => e.Status).HasDatabaseName("IX_BackupRecords_Status");
        builder.HasIndex(e => e.StartedAt).HasDatabaseName("IX_BackupRecords_StartedAt");
        builder.HasIndex(e => e.TriggerType).HasDatabaseName("IX_BackupRecords_TriggerType");

        builder.HasOne(e => e.TriggeredByUser)
            .WithMany()
            .HasForeignKey(e => e.TriggeredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

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
