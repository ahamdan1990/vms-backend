using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for getting visitors with pagination and filtering
/// </summary>
public class GetVisitorsQuery : IRequest<PagedResultDto<VisitorListDto>>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Company { get; set; }
    public bool? IsVip { get; set; }
    public bool? IsBlacklisted { get; set; }
    public bool? IsActive { get; set; } = true;
    public string SortBy { get; set; } = "FullName";
    public string SortDirection { get; set; } = "asc";
    public bool IncludeDeleted { get; set; } = false;
}
