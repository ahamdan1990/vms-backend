using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Controllers; // For UserActivityDto

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Query for getting user activity log
/// </summary>
public class GetUserActivityQuery : IRequest<PagedResultDto<UserActivityDto>>
{
    public int UserId { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public int Days { get; set; } = 30;
}