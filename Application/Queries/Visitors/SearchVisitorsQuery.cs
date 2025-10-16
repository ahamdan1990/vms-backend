using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for advanced visitor search
/// </summary>
public class SearchVisitorsQuery : IRequest<PagedResultDto<VisitorListDto>>
{
    public string? SearchTerm { get; set; }
    public string? Company { get; set; }
    public bool? IsVip { get; set; }
    public bool? IsBlacklisted { get; set; }
    public bool? IsActive { get; set; } = true;
    public string? Nationality { get; set; }
    public string? SecurityClearance { get; set; }
    public int? MinVisitCount { get; set; }
    public int? MaxVisitCount { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? LastVisitFrom { get; set; }
    public DateTime? LastVisitTo { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "FullName";
    public string SortDirection { get; set; } = "asc";
    public bool IncludeDeleted { get; set; } = false;
}
