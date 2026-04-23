using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Badges;

namespace VisitorManagementSystem.Api.Application.Queries.Badges;

/// <summary>
/// Generates badge content for the given invitation without sending it to a printer.
/// Returns the badge as a base64 PDF (and ZPL string if a label printer is configured)
/// so the frontend can display a preview or drive the print fallback flow.
/// </summary>
public record PreviewBadgeQuery(int InvitationId) : IRequest<BadgePreviewDto>;
