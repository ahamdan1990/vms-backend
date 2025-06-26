using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Base entity class that provides common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Date and time when the entity was created (UTC)
    /// </summary>
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the entity was last modified (UTC)
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Version timestamp for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Indicates whether the entity is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Updates the ModifiedOn timestamp
    /// </summary>
    public virtual void UpdateModifiedOn()
    {
        ModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the entity as inactive (soft delete)
    /// </summary>
    public virtual void Deactivate()
    {
        IsActive = false;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Marks the entity as active
    /// </summary>
    public virtual void Activate()
    {
        IsActive = true;
        UpdateModifiedOn();
    }
}