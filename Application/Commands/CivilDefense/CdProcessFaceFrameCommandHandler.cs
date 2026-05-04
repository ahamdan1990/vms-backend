using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdProcessFaceFrameCommandHandler : IRequestHandler<CdProcessFaceFrameCommand, CdFaceMatchResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFaceDetectionService _faceDetection;
    private readonly ILogger<CdProcessFaceFrameCommandHandler> _logger;

    public CdProcessFaceFrameCommandHandler(
        IUnitOfWork unitOfWork,
        IFaceDetectionService faceDetection,
        ILogger<CdProcessFaceFrameCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _faceDetection = faceDetection;
        _logger = logger;
    }

    public async Task<CdFaceMatchResultDto> Handle(CdProcessFaceFrameCommand request, CancellationToken cancellationToken)
    {
        if (!_faceDetection.IsAvailable)
            return new CdFaceMatchResultDto { FaceDetected = false, CameraId = request.CameraId, CameraRole = request.CameraRole.ToString() };

        List<RecognizedFace> recognized;
        try
        {
            using var ms = new MemoryStream(request.FrameBytes);
            recognized = await _faceDetection.RecognizeFacesAsync(ms, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CD face recognition failed for camera {CameraId}", request.CameraId);
            return new CdFaceMatchResultDto { FaceDetected = false, CameraId = request.CameraId, CameraRole = request.CameraRole.ToString() };
        }

        if (recognized == null || recognized.Count == 0)
            return new CdFaceMatchResultDto { FaceDetected = true, Matched = false, CameraId = request.CameraId, CameraRole = request.CameraRole.ToString() };

        var best = recognized.OrderByDescending(f => f.Similarity).First();
        if (best.Similarity < request.ConfidenceThreshold)
            return new CdFaceMatchResultDto { FaceDetected = true, Matched = false, Similarity = best.Similarity, CameraId = request.CameraId, CameraRole = request.CameraRole.ToString() };

        // Resolve visitor by SubjectId (visitor email or synthetic cd.local email)
        var visitor = await _unitOfWork.Visitors.GetByEmailAsync(best.SubjectId, cancellationToken);

        // Check active invitation
        int? activeInvitationId = null;
        bool isCurrentlyInside = false;
        if (visitor != null)
        {
            var activeInv = await _unitOfWork.Invitations
                .GetByVisitorIdAndStatusAsync(visitor.Id, InvitationStatus.Active, cancellationToken);
            var active = activeInv.FirstOrDefault();
            activeInvitationId = active?.Id;
            isCurrentlyInside = active != null;
        }

        return new CdFaceMatchResultDto
        {
            FaceDetected       = true,
            Matched            = visitor != null,
            Similarity         = best.Similarity,
            Confidence         = best.Confidence,
            VisitorId          = visitor?.Id,
            VisitorName        = visitor?.FullName,
            PhoneNumber        = visitor?.PhoneNumber?.Value,
            NationalId         = visitor?.GovernmentId,
            IsCurrentlyInside  = isCurrentlyInside,
            ActiveInvitationId = activeInvitationId,
            IsBlacklisted      = visitor?.IsBlacklisted ?? false,
            IsStaff            = false,
            CameraId           = request.CameraId,
            CameraRole         = request.CameraRole.ToString(),
        };
    }
}
