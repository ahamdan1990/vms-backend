using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for soft delete entities
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public abstract class SoftDeleteEntityConfiguration<TEntity> : AuditableEntityConfiguration<TEntity>
    where TEntity : SoftDeleteEntity
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder)
    {
        // Soft delete properties
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedOn)
            .IsRequired(false);

        builder.Property(e => e.DeletedBy)
            .IsRequired(false);

        // Deleted by relationship
        builder.HasOne(e => e.DeletedByUser)
            .WithMany()
            .HasForeignKey(e => e.DeletedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName($"FK_{typeof(TEntity).Name}_DeletedBy_User");

        // Indexes for soft delete queries
        builder.HasIndex(e => e.IsDeleted)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsDeleted");

        builder.HasIndex(e => new { e.IsDeleted, e.DeletedOn })
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsDeleted_DeletedOn");

        builder.HasIndex(e => e.DeletedBy)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_DeletedBy");

        // Configure entity-specific soft delete properties
        ConfigureSoftDeleteEntity(builder);
    }

    /// <summary>
    /// Override this method to configure soft delete entity-specific properties
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    protected virtual void ConfigureSoftDeleteEntity(EntityTypeBuilder<TEntity> builder)
    {
        // Default implementation - can be overridden by derived classes
    }
}
