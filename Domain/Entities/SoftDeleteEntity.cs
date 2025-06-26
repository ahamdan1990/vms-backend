namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Base entity class that provides soft delete functionality
/// </summary>
public abstract class SoftDeleteEntity : AuditableEntity
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Date and time when the entity was soft deleted (UTC)
    /// </summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>
    /// ID of the user who soft deleted the entity
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Navigation property for the user who soft deleted the entity
    /// </summary>
    public virtual User? DeletedByUser { get; set; }

    /// <summary>
    /// Soft deletes the entity
    /// </summary>
    /// <param name="deletedBy">ID of the user performing the deletion</param>
    public virtual void SoftDelete(int deletedBy)
    {
        IsDeleted = true;
        DeletedOn = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
        UpdateModifiedBy(deletedBy);
    }

    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    /// <param name="restoredBy">ID of the user performing the restoration</param>
    public virtual void Restore(int restoredBy)
    {
        IsDeleted = false;
        DeletedOn = null;
        DeletedBy = null;
        IsActive = true;
        UpdateModifiedBy(restoredBy);
    }

    /// <summary>
    /// Checks if the entity is currently deleted
    /// </summary>
    /// <returns>True if the entity is soft deleted</returns>
    public virtual bool IsCurrentlyDeleted()
    {
        return IsDeleted && DeletedOn.HasValue;
    }

    /// <summary>
    /// Gets the number of days since the entity was deleted
    /// </summary>
    /// <returns>Number of days since deletion, or null if not deleted</returns>
    public virtual int? DaysSinceDeleted()
    {
        if (!IsDeleted || !DeletedOn.HasValue)
            return null;

        return (int)(DateTime.UtcNow - DeletedOn.Value).TotalDays;
    }
}