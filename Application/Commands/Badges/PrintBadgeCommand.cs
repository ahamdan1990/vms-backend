using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Badges;

namespace VisitorManagementSystem.Api.Application.Commands.Badges;

/// <summary>
/// Attempts to print a visitor badge for the given invitation.
///
/// The handler follows the three-tier fallback strategy:
///   Tier 1 — backend sends the badge directly to the configured network printer.
///   Tier 2 — if the backend cannot reach the printer, the PDF/ZPL is returned in the
///             response so the frontend can forward it to the local bridge service.
///   Tier 3 — if the bridge is also unreachable, the frontend uses window.print().
///
/// The <see cref="BadgePrintResultDto.PdfBase64"/> and <see cref="BadgePrintResultDto.ZplContent"/>
/// fields are always populated so the frontend can drive fallback without a second round-trip.
/// </summary>
public record PrintBadgeCommand(int InvitationId, int PrintedBy) : IRequest<BadgePrintResultDto>;
