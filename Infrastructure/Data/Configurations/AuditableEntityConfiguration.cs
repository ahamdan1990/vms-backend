using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for auditable entities
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public abstract class AuditableEntityConfiguration<TEntity> : BaseEntityConfiguration<TEntity>
    where TEntity : AuditableEntity
{
    protected override void ConfigureDerivedEntity(EntityTypeBuilder<TEntity> builder)
    {
        // Created by relationship
        builder.Property(e => e.CreatedBy)
            .IsRequired(false);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName($"FK_{typeof(TEntity).Name}_CreatedBy_User");

        // Modified by relationship
        builder.Property(e => e.ModifiedBy)
            .IsRequired(false);

        builder.HasOne(e => e.ModifiedByUser)
            .WithMany()
            .HasForeignKey(e => e.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName($"FK_{typeof(TEntity).Name}_ModifiedBy_User");

        // Indexes for audit fields
        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedBy");

        builder.HasIndex(e => e.ModifiedBy)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_ModifiedBy");

        // Configure entity-specific auditable properties
        ConfigureAuditableEntity(builder);
    }

    /// <summary>
    /// Override this method to configure auditable entity-specific properties
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    protected virtual void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder)
    {
        // Default implementation - can be overridden by derived classes
    }
}