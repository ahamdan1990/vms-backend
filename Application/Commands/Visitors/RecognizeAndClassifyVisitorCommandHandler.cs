using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handles RecognizeAndClassifyVisitorCommand:
///   1. Run CompreFace recognition
///   2. Look up the matching visitor in the DB
///   3. Check IsBlacklisted / IsVip
///   4. Fire the appropriate alert (routes to configured recipients via NotificationService)
///   5. Return a rich result so the frontend can show the right UI immediately
/// </summary>
public class RecognizeAndClassifyVisitorCommandHandler
    : IRequestHandler<RecognizeAndClassifyVisitorCommand, RecognitionResultDto>
{
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RecognizeAndClassifyVisitorCommandHandler> _logger;

    public RecognizeAndClassifyVisitorCommandHandler(
        IFaceDetectionService faceDetectionService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RecognizeAndClassifyVisitorCommandHandler> logger)
    {
        _faceDetectionService = faceDetectionService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RecognitionResultDto> Handle(
        RecognizeAndClassifyVisitorCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Run face recognition ────────────────────────────────────────────
        using var stream = request.Photo.OpenReadStream();
        List<RecognizedFace> recognized;

        try
        {
            recognized = await _faceDetectionService.RecognizeFacesAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Face recognition service unavailable during walk-in capture");
            return new RecognitionResultDto { IsRecognized = false };
        }

        if (recognized == null || !recognized.Any())
            return new RecognitionResultDto { IsRecognized = false };

        var best = recognized.OrderByDescending(f => f.Similarity).First();

        // ── 2. Look up visitor in DB ───────────────────────────────────────────
        var visitor = await _unitOfWork.Visitors.GetByEmailAsync(best.SubjectId, cancellationToken);

        if (visitor == null)
        {
            // Fallback: search by name (underscores used as space separator in CompreFace subject IDs)
            var nameParts = best.SubjectId.Replace("_", " ").Split(' ', 2);
            if (nameParts.Length >= 2)
            {
                var fallback = await _unitOfWork.Visitors.SearchVisitorsAsync(
                    $"{nameParts[0]} {nameParts[1]}", 0, 5, cancellationToken: cancellationToken);
                visitor = fallback.Visitors?.FirstOrDefault();
            }
        }

        if (visitor == null)
        {
            _logger.LogWarning("Face recognized as {SubjectId} but no visitor found in DB", best.SubjectId);
            return new RecognitionResultDto { IsRecognized = false };
        }

        // ── 3. Build base result ───────────────────────────────────────────────
        var result = new RecognitionResultDto
        {
            IsRecognized = true,
            Similarity = best.Similarity,
            Visitor = _mapper.Map<VisitorDto>(visitor),
            IsBlacklisted = visitor.IsBlacklisted,
            IsVip = visitor.IsVip,
            BlacklistReason = visitor.BlacklistReason
        };

        // ── 4. Fire classification alerts ─────────────────────────────────────
        if (visitor.IsBlacklisted)
        {
            _logger.LogWarning(
                "Blacklisted visitor {VisitorId} ({Name}) detected at {Location} during walk-in capture",
                visitor.Id, visitor.FullName, request.Location);

            try
            {
                await _notificationService.NotifyBlacklistDetectionAsync(
                    $"{visitor.FullName} (ID: {visitor.Id})",
                    request.Location,
                    visitorId: visitor.Id,
                    cancellationToken: cancellationToken);

                result.AlertTriggered = true;
                result.AlertMessage =
                    $"SECURITY ALERT: {visitor.FullName} is on the blacklist. Reason: {visitor.BlacklistReason}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send blacklist alert for visitor {VisitorId}", visitor.Id);
            }
        }
        else if (visitor.IsVip)
        {
            _logger.LogInformation(
                "VIP visitor {VisitorId} ({Name}) recognised at {Location}",
                visitor.Id, visitor.FullName, request.Location);

            try
            {
                await _notificationService.NotifyVipArrivalAsync(
                    visitor.FullName,
                    request.Location,
                    visitorId: visitor.Id,
                    cancellationToken: cancellationToken);

                result.AlertTriggered = true;
                result.AlertMessage = $"VIP visitor {visitor.FullName} has arrived at {request.Location}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send VIP alert for visitor {VisitorId}", visitor.Id);
            }
        }

        _logger.LogInformation(
            "Walk-in recognition complete: visitor {VisitorId} ({Name}), " +
            "IsVip={IsVip}, IsBlacklisted={IsBlacklisted}, AlertTriggered={AlertTriggered}",
            visitor.Id, visitor.FullName, visitor.IsVip, visitor.IsBlacklisted, result.AlertTriggered);

        return result;
    }
}
