using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Commands.Cameras;

/// <summary>
/// Command for deleting a camera from the visitor management system
/// Supports both soft delete (default) and permanent deletion with comprehensive validation
/// </summary>
public class DeleteCameraCommand : IRequest<ApiResponseDto<object>>
{
    /// <summary>
    /// Camera ID to delete (required)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of the user performing the deletion (for audit trail)
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to perform permanent deletion instead of soft delete
    /// Permanent deletion cannot be reversed and should be used with extreme caution
    /// </summary>
    public bool PermanentDelete { get; set; } = false;

    /// <summary>
    /// Optional reason for deletion (recommended for audit purposes)
    /// </summary>
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Force deletion even if camera has dependencies or is actively being used
    /// Use with extreme caution as this may cause system instability
    /// </summary>
    public bool ForceDelete { get; set; } = false;
}