using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Queries.Cameras;

/// <summary>
/// Query for advanced camera search with comprehensive filtering options
/// Supports complex search scenarios and detailed filtering requirements
/// </summary>
public class SearchCamerasQuery : IRequest<PagedResultDto<CameraListDto>>
{
    /// <summary>
    /// Camera search criteria with all filtering options
    /// </summary>
    public CameraSearchDto SearchCriteria { get; set; } = new();
}