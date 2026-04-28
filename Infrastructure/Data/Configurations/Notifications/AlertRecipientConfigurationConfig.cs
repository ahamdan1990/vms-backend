using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations.Notifications;

public class AlertRecipientConfigurationConfig : IEntityTypeConfiguration<AlertRecipientConfiguration>
{
    public void Configure(EntityTypeBuilder<AlertRecipientConfiguration> builder)
    {
        builder.ToTable("AlertRecipientConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AlertType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(e => e.TargetType)
            .IsRequired()
            .HasMaxLength(10); // "Role" | "User"

        builder.Property(e => e.TargetRole)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedOn).IsRequired();
        builder.Property(e => e.ModifiedOn);

        // Soft-delete via IsActive (matches other entities in the project)
        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasQueryFilter(e => e.IsActive);

        builder.Property(e => e.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(e => e.AlertType)
            .HasDatabaseName("IX_AlertRecipientConfigurations_AlertType");

        builder.HasIndex(e => new { e.AlertType, e.IsEnabled })
            .HasDatabaseName("IX_AlertRecipientConfigurations_AlertType_IsEnabled");

        // Relationships
        builder.HasOne(e => e.TargetUser)
            .WithMany()
            .HasForeignKey(e => e.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.ModifiedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
