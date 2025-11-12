using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Query for searching hosts across local staff users and (optionally) the corporate directory.
/// </summary>
public class SearchHostsQuery : IRequest<List<HostSearchResultDto>>
{
    public string SearchTerm { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
    public bool IncludeDirectory { get; set; } = true;
}
