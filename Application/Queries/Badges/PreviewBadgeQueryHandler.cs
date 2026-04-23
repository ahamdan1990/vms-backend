using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Badges;
using VisitorManagementSystem.Api.Application.Services.Badge;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Badges;

public sealed class PreviewBadgeQueryHandler : IRequestHandler<PreviewBadgeQuery, BadgePreviewDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBadgeService _badgeService;
    private readonly ILogger<PreviewBadgeQueryHandler> _logger;

    public PreviewBadgeQueryHandler(
        IUnitOfWork unitOfWork,
        IBadgeService badgeService,
        ILogger<PreviewBadgeQueryHandler> logger)
    {
        _unitOfWork   = unitOfWork;
        _badgeService = badgeService;
        _logger       = logger;
    }

    public async Task<BadgePreviewDto> Handle(PreviewBadgeQuery request, CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
                      ?? throw new KeyNotFoundException($"Invitation {request.InvitationId} not found.");

        // Load visitor photo bytes if available
        byte[]? photoBytes = null;
        if (!string.IsNullOrWhiteSpace(invitation.Visitor?.ProfilePhotoPath))
        {
            try
            {
                photoBytes = await File.ReadAllBytesAsync(
                    invitation.Visitor.ProfilePhotoPath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load visitor photo for badge. Path: {Path}",
                    invitation.Visitor.ProfilePhotoPath);
            }
        }

        var pdfBytes = await _badgeService.GenerateBadgePdfAsync(invitation, photoBytes, cancellationToken);

        return new BadgePreviewDto
        {
            PdfBase64       = Convert.ToBase64String(pdfBytes),
            ContentType     = "application/pdf",
            FileSizeBytes   = pdfBytes.Length,
            InvitationId    = invitation.Id,
            InvitationNumber = invitation.InvitationNumber,
        };
    }
}
