using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Location
/// </summary>
public class LocationConfiguration : AuditableEntityConfiguration<Location>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        // Basic properties
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.LocationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Floor)
            .HasMaxLength(10);

        builder.Property(l => l.Building)
            .HasMaxLength(50);

        builder.Property(l => l.Room)
            .HasMaxLength(20);

        builder.Property(l => l.DisplayOrder)
            .HasDefaultValue(1);

        builder.Property(l => l.MaxOccupancy)
            .HasDefaultValue(1);

        builder.Property(l => l.RequiresSecurityClearance)
            .HasDefaultValue(false);

        builder.Property(l => l.SecurityClearanceLevel)
            .HasMaxLength(50);

        builder.Property(l => l.IsAccessible)
            .HasDefaultValue(true);

        builder.Property(l => l.AccessInstructions)
            .HasMaxLength(1000);

        // Self-referencing relationship
        builder.HasOne(l => l.ParentLocation)
            .WithMany(l => l.ChildLocations)
            .HasForeignKey(l => l.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(l => l.Code)
            .HasDatabaseName("IX_Locations_Code")
            .IsUnique();

        builder.HasIndex(l => l.Name)
            .HasDatabaseName("IX_Locations_Name");

        builder.HasIndex(l => l.LocationType)
            .HasDatabaseName("IX_Locations_LocationType");

        builder.HasIndex(l => l.ParentLocationId)
            .HasDatabaseName("IX_Locations_ParentLocationId");

        builder.HasIndex(l => l.DisplayOrder)
            .HasDatabaseName("IX_Locations_DisplayOrder");

        builder.HasIndex(l => l.SecurityClearanceLevel)
            .HasDatabaseName("IX_Locations_SecurityClearanceLevel");
    }
}
