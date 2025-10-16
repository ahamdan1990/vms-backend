using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Query for getting paginated list of users with filtering and sorting
/// </summary>
public class GetUsersQuery : IRequest<PagedResultDto<UserListDto>>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public string? Department { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public bool IncludeInactive { get; set; } = false;
}
