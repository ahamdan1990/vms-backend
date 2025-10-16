using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;

/// <summary>
/// Visit purpose data transfer object
/// </summary>
public class VisitPurposeDto
{
    /// <summary>
    /// Purpose ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Purpose name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Purpose code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Purpose description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon for UI display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Color for UI display
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this purpose requires approval
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Maximum duration in hours
    /// </summary>
    public int MaxDurationHours { get; set; }

    /// <summary>
    /// Whether purpose is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Created by user ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// Modified by user ID
    /// </summary>
    public int? ModifiedBy { get; set; }

    /// <summary>
    /// Created by user name
    /// </summary>
    public string? CreatedByUser { get; set; }

    /// <summary>
    /// Modified by user name
    /// </summary>
    public string? ModifiedByUser { get; set; }

    /// <summary>
    /// Modification date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Whether this purpose is soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Deletion date
    /// </summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>
    /// Deleted by user ID
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Deleted by user name
    /// </summary>
    public string? DeletedByUser { get; set; }

    /// <summary>
    /// Related invitations count
    /// </summary>
    public int InvitationsCount { get; set; }
}

/// <summary>
/// DTO for creating a visit purpose
/// </summary>
public class CreateVisitPurposeDto
{
    /// <summary>
    /// Visit purpose name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Visit purpose description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this purpose requires approval
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether this purpose is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Color code for UI display
    /// </summary>
    [MaxLength(7)]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Icon name for UI display
    /// </summary>
    [MaxLength(50)]
    public string? IconName { get; set; }
}

/// <summary>
/// DTO for updating a visit purpose
/// </summary>
public class UpdateVisitPurposeDto
{
    /// <summary>
    /// Visit purpose name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Visit purpose description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this purpose requires approval
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether this purpose is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Color code for UI display
    /// </summary>
    [MaxLength(7)]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Icon name for UI display
    /// </summary>
    [MaxLength(50)]
    public string? IconName { get; set; }
}
