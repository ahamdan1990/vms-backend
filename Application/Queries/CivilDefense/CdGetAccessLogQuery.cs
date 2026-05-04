using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class CdGetAccessLogQuery : IRequest<List<CdAccessLogEntryDto>>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? StaffOnly { get; set; }
    public bool? VisitorsOnly { get; set; }
    public string? SearchName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
