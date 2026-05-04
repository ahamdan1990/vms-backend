using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Queries.CivilDefense;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers.CivilDefense;

[Authorize]
[ApiController]
[Route("api/civil-defense/reports")]
public class CdReportsController : BaseController
{
    private readonly IMediator _mediator;

    public CdReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("access-log")]
    [Authorize(Policy = Permissions.CivilDefense.ViewReports)]
    public async Task<IActionResult> GetAccessLog(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? staffOnly,
        [FromQuery] bool? visitorsOnly,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CdGetAccessLogQuery
        {
            From         = from,
            To           = to,
            StaffOnly    = staffOnly,
            VisitorsOnly = visitorsOnly,
            SearchName   = search,
            Page         = page,
            PageSize     = pageSize,
        }, ct);

        return SuccessResponse(result);
    }
}
