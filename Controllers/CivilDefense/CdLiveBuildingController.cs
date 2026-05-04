using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Queries.CivilDefense;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers.CivilDefense;

[Authorize]
[ApiController]
[Route("api/civil-defense/live")]
public class CdLiveBuildingController : BaseController
{
    private readonly IMediator _mediator;

    public CdLiveBuildingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("occupancy")]
    [Authorize(Policy = Permissions.CivilDefense.ViewLiveBuilding)]
    public async Task<IActionResult> GetLiveOccupancy(CancellationToken ct)
    {
        var result = await _mediator.Send(new CdGetLiveBuildingOccupancyQuery(), ct);
        return SuccessResponse(result);
    }

    [HttpGet("access-log")]
    [Authorize(Policy = Permissions.CivilDefense.ViewLiveBuilding)]
    public async Task<IActionResult> GetAccessLog(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? staffOnly,
        [FromQuery] bool? visitorsOnly,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
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

    [HttpGet("search-visitor")]
    [Authorize(Policy = Permissions.CivilDefense.CheckIn)]
    public async Task<IActionResult> SearchVisitor([FromQuery] string q, [FromQuery] int max = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CdSearchVisitorQuery { SearchTerm = q, MaxResults = max }, ct);
        return SuccessResponse(result);
    }
}
