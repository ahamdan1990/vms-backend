using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.CivilDefense;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers.CivilDefense;

[Authorize]
[ApiController]
[Route("api/civil-defense/check-in-out")]
public class CdCheckInOutController : BaseController
{
    private readonly IMediator _mediator;

    public CdCheckInOutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("visitor/check-in")]
    [Authorize(Policy = Permissions.CivilDefense.CheckIn)]
    public async Task<IActionResult> CheckInVisitor([FromBody] CdCheckInVisitorRequest request, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        if (operatorId == null) return Unauthorized();

        var command = new CdCheckInVisitorCommand
        {
            InvitationId   = request.InvitationId,
            OperatorUserId = operatorId.Value,
            CameraId       = request.CameraId,
            Force          = request.Force,
        };

        var result = await _mediator.Send(command, ct);
        return SuccessResponse(result, "Visitor checked in.");
    }

    [HttpPost("visitor/check-out")]
    [Authorize(Policy = Permissions.CivilDefense.CheckOut)]
    public async Task<IActionResult> CheckOutVisitor([FromBody] CdCheckOutVisitorRequest request, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        if (operatorId == null) return Unauthorized();

        var command = new CdCheckOutVisitorCommand
        {
            InvitationId   = request.InvitationId,
            OperatorUserId = operatorId.Value,
        };

        var result = await _mediator.Send(command, ct);
        return SuccessResponse(result, "Visitor checked out.");
    }

    [HttpPost("visitor/register-and-check-in")]
    [Authorize(Policy = Permissions.CivilDefense.RegisterVisitor)]
    public async Task<IActionResult> RegisterAndCheckIn([FromBody] CdRegisterAndCheckInCommand command, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        if (operatorId == null) return Unauthorized();

        command.OperatorUserId = operatorId.Value;
        var result = await _mediator.Send(command, ct);
        return SuccessResponse(result, result.WasExistingVisitor ? "Existing visitor checked in." : "New visitor registered and checked in.");
    }

    [HttpPost("staff/check-in")]
    [Authorize(Policy = Permissions.CivilDefense.StaffCheckIn)]
    public async Task<IActionResult> StaffCheckIn([FromBody] CdStaffCheckInCommand command, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        if (operatorId == null) return Unauthorized();

        command.ProcessedByUserId = operatorId.Value;
        var result = await _mediator.Send(command, ct);
        return SuccessResponse(result, "Staff member checked in.");
    }

    [HttpPost("staff/check-out")]
    [Authorize(Policy = Permissions.CivilDefense.StaffCheckIn)]
    public async Task<IActionResult> StaffCheckOut([FromBody] CdStaffCheckOutCommand command, CancellationToken ct)
    {
        var operatorId = GetCurrentUserId();
        if (operatorId == null) return Unauthorized();

        command.ProcessedByUserId = operatorId.Value;
        var result = await _mediator.Send(command, ct);
        return SuccessResponse(result, "Staff member checked out.");
    }
}

public record CdCheckInVisitorRequest(int InvitationId, int? CameraId = null, bool Force = false);
public record CdCheckOutVisitorRequest(int InvitationId);
