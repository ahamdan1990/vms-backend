using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Badges;
using VisitorManagementSystem.Api.Application.DTOs.Badges;
using VisitorManagementSystem.Api.Application.Queries.Badges;
using VisitorManagementSystem.Api.Application.Services.Printing;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Endpoints for generating and printing visitor badges.
///
/// Print flow (three-tier fallback):
///   1. POST /api/badges/print/{invitationId}
///      → backend tries IPP/ZPL directly. If it succeeds: done.
///      → If it fails (no printer configured / unreachable), the response still
///        contains PdfBase64 + ZplContent so the client can fall through.
///   2. Client calls bridge POST /print-badge (localhost:7891) with the returned PDF/ZPL.
///   3. If bridge is unavailable, client opens the PDF in a new window for window.print().
/// </summary>
[ApiController]
[Route("api/badges")]
[Authorize]
public sealed class BadgesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IPrinterService _printerService;

    public BadgesController(IMediator mediator, IPrinterService printerService)
    {
        _mediator        = mediator;
        _printerService  = printerService;
    }

    /// <summary>
    /// Generates a badge PDF preview without printing.
    /// Returns base64-encoded PDF suitable for a browser preview or fallback print.
    /// </summary>
    [HttpGet("preview/{invitationId:int}")]
    [ProducesResponseType(typeof(BadgePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewBadge(
        int invitationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new PreviewBadgeQuery(invitationId), cancellationToken);
            return SuccessResponse(dto, "Badge preview generated.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return ServerErrorResponse($"Failed to generate badge preview: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints a visitor badge.
    /// Tier 1: tries the server-side printer (IPP or ZPL via SystemConfigurations).
    /// Always returns PdfBase64 + ZplContent so the client can fall through to
    /// bridge printing (tier 2) or browser window.print() (tier 3).
    /// </summary>
    [HttpPost("print/{invitationId:int}")]
    [ProducesResponseType(typeof(BadgePrintResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PrintBadge(
        int invitationId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return UnauthorizedResponse();

        try
        {
            var dto = await _mediator.Send(
                new PrintBadgeCommand(invitationId, userId.Value), cancellationToken);

            return SuccessResponse(dto, dto.Success
                ? "Badge sent to printer."
                : "Backend printer unavailable — use fallback.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return ServerErrorResponse($"Failed to print badge: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests whether the configured server-side printer is reachable.
    /// Returns { available: true/false }.
    /// </summary>
    [HttpGet("hardware/test-printer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestPrinter(CancellationToken cancellationToken)
    {
        var available = await _printerService.IsAvailableAsync(cancellationToken);
        return SuccessResponse(new { available }, available
            ? "Printer is reachable."
            : "Printer is not reachable or not configured.");
    }
}
