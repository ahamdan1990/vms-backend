using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Badges;
using VisitorManagementSystem.Api.Application.Services.Badge;
using VisitorManagementSystem.Api.Application.Services.Printing;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Badges;

public sealed class PrintBadgeCommandHandler : IRequestHandler<PrintBadgeCommand, BadgePrintResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBadgeService _badgeService;
    private readonly IPrinterService _printerService;
    private readonly ILogger<PrintBadgeCommandHandler> _logger;

    public PrintBadgeCommandHandler(
        IUnitOfWork unitOfWork,
        IBadgeService badgeService,
        IPrinterService printerService,
        ILogger<PrintBadgeCommandHandler> logger)
    {
        _unitOfWork      = unitOfWork;
        _badgeService    = badgeService;
        _printerService  = printerService;
        _logger          = logger;
    }

    public async Task<BadgePrintResultDto> Handle(PrintBadgeCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
                      ?? throw new KeyNotFoundException($"Invitation {request.InvitationId} not found.");

        // Load visitor photo
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
                _logger.LogWarning(ex, "Could not load visitor photo for badge.");
            }
        }

        // Generate badge content (always — needed for fallback regardless of print outcome)
        var pdfBytes   = await _badgeService.GenerateBadgePdfAsync(invitation, photoBytes, cancellationToken);
        var zplContent = _badgeService.GenerateBadgeZpl(invitation);
        var jobName    = $"Visitor Badge – {invitation.Visitor?.FullName} – {invitation.InvitationNumber}";

        var dto = new BadgePrintResultDto
        {
            InvitationId     = invitation.Id,
            InvitationNumber = invitation.InvitationNumber,
            PdfBase64        = Convert.ToBase64String(pdfBytes),
            ZplContent       = zplContent,
        };

        // ── Tier 1: try to print from the backend server ──────────────────────
        var available = await _printerService.IsAvailableAsync(cancellationToken);

        if (!available)
        {
            _logger.LogInformation(
                "No server-side printer available for invitation {Id}. Returning badge for client-side fallback.",
                invitation.Id);

            dto.Success      = false;
            dto.ErrorMessage = "No server-side printer configured. Use the bridge or browser print fallback.";
            return dto;
        }

        // Try ZPL first (smaller, faster, better label quality)
        var zplResult = await _printerService.PrintZplAsync(zplContent, cancellationToken);
        if (zplResult.Success)
        {
            _logger.LogInformation("Badge printed via ZPL for invitation {Id}", invitation.Id);
            dto.Success  = true;
            dto.Protocol = zplResult.Protocol;
            return dto;
        }

        // Fall back to PDF/IPP
        var pdfResult = await _printerService.PrintPdfAsync(pdfBytes, jobName, cancellationToken);
        if (pdfResult.Success)
        {
            _logger.LogInformation("Badge printed via IPP for invitation {Id}", invitation.Id);
            dto.Success  = true;
            dto.Protocol = pdfResult.Protocol;
            return dto;
        }

        _logger.LogWarning("Backend printing failed for invitation {Id}: ZPL={ZplErr}, IPP={IppErr}",
            invitation.Id, zplResult.ErrorMessage, pdfResult.ErrorMessage);

        dto.Success      = false;
        dto.ErrorMessage = $"Backend print failed. {pdfResult.ErrorMessage} Returning badge for client-side fallback.";
        return dto;
    }
}
