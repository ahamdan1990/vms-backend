using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.CivilDefense;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers.CivilDefense;

[Authorize]
[ApiController]
[Route("api/civil-defense/cameras")]
public class CdCameraController : BaseController
{
    private readonly IMediator _mediator;

    public CdCameraController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Accepts a single JPEG frame from a client (e.g. browser webcam) and runs face recognition.
    /// This is the manual/client-push path; the background service handles automated camera sampling.
    /// </summary>
    [HttpPost("{cameraId:int}/process-frame")]
    [Authorize(Policy = Permissions.CivilDefense.ProcessFaceFrame)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB per frame
    public async Task<IActionResult> ProcessFrame(
        int cameraId,
        IFormFile frame,
        [FromQuery] double threshold = 0.80,
        CancellationToken ct = default)
    {
        if (frame == null || frame.Length == 0)
            return BadRequestResponse("No frame data provided.");

        using var ms = new MemoryStream();
        await frame.CopyToAsync(ms, ct);

        var result = await _mediator.Send(new CdProcessFaceFrameCommand
        {
            FrameBytes          = ms.ToArray(),
            CameraId            = cameraId,
            CameraRole          = CameraRole.None,
            ConfidenceThreshold = threshold,
        }, ct);

        return SuccessResponse(result);
    }
}
