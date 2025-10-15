using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;

namespace VisitorManagementSystem.Api.Application.Queries.Cameras;

/// <summary>
/// Query for retrieving a single camera by its unique identifier
/// Supports inclusion of deleted cameras for administrative purposes
/// </summary>
public class GetCameraByIdQuery : IRequest<CameraDto?>
{
    /// <summary>
    /// Camera unique identifier (required)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Whether to include deleted cameras in the search
    /// Defaults to false for normal operations
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Whether to include sensitive information (credentials, etc.) in the result
    /// Should only be true for administrative operations with proper permissions
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = false;
}