using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Base entity class that provides audit trail functionality
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>
    /// ID of the user who created the entity
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// ID of the user who last modified the entity
    /// </summary>
    public int? ModifiedBy { get; set; }

    /// <summary>
    /// Navigation property for the user who created the entity
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property for the user who last modified the entity
    /// </summary>
    public virtual User? ModifiedByUser { get; set; }

    /// <summary>
    /// Updates the entity with modification details
    /// </summary>
    /// <param name="modifiedBy">ID of the user making the modification</param>
    public virtual void UpdateModifiedBy(int modifiedBy)
    {
        ModifiedBy = modifiedBy;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Sets the entity creation details
    /// </summary>
    /// <param name="createdBy">ID of the user creating the entity</param>
    public virtual void SetCreatedBy(int createdBy)
    {
        CreatedBy = createdBy;
        CreatedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the entity with user information
    /// </summary>
    /// <param name="deactivatedBy">ID of the user deactivating the entity</param>
    public virtual void Deactivate(int deactivatedBy)
    {
        base.Deactivate();
        UpdateModifiedBy(deactivatedBy);
    }

    /// <summary>
    /// Activates the entity with user information
    /// </summary>
    /// <param name="activatedBy">ID of the user activating the entity</param>
    public virtual void Activate(int activatedBy)
    {
        base.Activate();
        UpdateModifiedBy(activatedBy);
    }
}