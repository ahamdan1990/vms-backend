using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Base entity configuration for common properties
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Common properties
        builder.Property(e => e.CreatedOn)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.ModifiedOn)
            .IsRequired(false);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Row version for optimistic concurrency
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Indexes for common queries
        builder.HasIndex(e => e.CreatedOn)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedOn");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsActive");

        builder.HasIndex(e => new { e.IsActive, e.CreatedOn })
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsActive_CreatedOn");

        // Configure derived properties if needed
        ConfigureDerivedEntity(builder);
    }

    /// <summary>
    /// Override this method to configure entity-specific properties
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    protected abstract void ConfigureDerivedEntity(EntityTypeBuilder<TEntity> builder);
}