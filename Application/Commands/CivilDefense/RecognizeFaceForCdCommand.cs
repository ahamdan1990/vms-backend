using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class RecognizeFaceForCdCommand : IRequest<CdRecognitionResultDto>
{
    public IFormFile Photo { get; set; } = null!;
}

public class CdRecognitionResultDto
{
    public bool IsRecognized { get; set; }
    public double? Similarity { get; set; }
    public string PersonType { get; set; } = "Unknown"; // "Visitor" | "Staff" | "Unknown"
    public int? PersonId { get; set; }
    public string? FullName { get; set; }
    public string? Department { get; set; }
    public string? Company { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsCurrentlyInside { get; set; }
    public bool IsTempAway { get; set; }
    public int? StaffPresenceId { get; set; }
    public int? ActiveInvitationId { get; set; }
    public int? FaceX { get; set; }
    public int? FaceY { get; set; }
    public int? FaceWidth { get; set; }
    public int? FaceHeight { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
}

public class RecognizeFaceForCdCommandHandler
    : IRequestHandler<RecognizeFaceForCdCommand, CdRecognitionResultDto>
{
    private readonly IFaceDetectionService _faceDetection;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;
    private readonly ILogger<RecognizeFaceForCdCommandHandler> _logger;

    public RecognizeFaceForCdCommandHandler(
        IFaceDetectionService faceDetection,
        IUnitOfWork unitOfWork,
        IUrlResolverService urlResolver,
        ILogger<RecognizeFaceForCdCommandHandler> logger)
    {
        _faceDetection = faceDetection;
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
        _logger = logger;
    }

    public async Task<CdRecognitionResultDto> Handle(
        RecognizeFaceForCdCommand request,
        CancellationToken cancellationToken)
    {
        // Read bytes once — the stream is not seekable for reuse
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await request.Photo.OpenReadStream().CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }

        // 1. Run face recognition
        List<RecognizedFace> recognized;
        try
        {
            using var stream1 = new MemoryStream(bytes);
            recognized = await _faceDetection.RecognizeFacesAsync(stream1, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Face recognition unavailable during CD manual capture");
            return new CdRecognitionResultDto { IsRecognized = false };
        }

        if (recognized == null || recognized.Count == 0)
        {
            // Detection-only fallback: still return face location for cropping
            DetectedFace? det = null;
            try
            {
                using var stream2 = new MemoryStream(bytes);
                var detected = await _faceDetection.DetectFacesAsync(stream2, cancellationToken);
                det = detected?.FirstOrDefault();
            }
            catch { }
            return new CdRecognitionResultDto
            {
                IsRecognized = false,
                FaceX = det?.X, FaceY = det?.Y,
                FaceWidth = det?.Width, FaceHeight = det?.Height,
            };
        }

        // 2. Read recognition threshold from system config (default 0.9)
        var thresholdConfig = await _unitOfWork.SystemConfigurations
            .GetByCategoryAndKeyAsync("FRSystem", "RecognitionThreshold", cancellationToken);
        var recognitionThreshold = double.TryParse(
            thresholdConfig?.Value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var parsedThreshold) ? parsedThreshold : 0.9;

        var best = recognized.OrderByDescending(f => f.Similarity).First();

        if (best.Similarity < recognitionThreshold)
        {
            var belowBox = best.BoundingBox;
            return new CdRecognitionResultDto
            {
                IsRecognized = false,
                FaceX = belowBox?.X, FaceY = belowBox?.Y,
                FaceWidth = belowBox?.Width, FaceHeight = belowBox?.Height,
            };
        }

        // 3. Determine person type from subject ID prefix
        if (!TryParseSubject(best.SubjectId, out var personType, out var personId))
        {
            _logger.LogWarning("CD recognition: unrecognized subject ID format '{SubjectId}'", best.SubjectId);
            return new CdRecognitionResultDto { IsRecognized = false };
        }

        var box = best.BoundingBox;

        // 3. Look up visitor or staff
        if (personType == "visitor")
            return await BuildVisitorResult(personId, best.Similarity, box, cancellationToken);

        if (personType is "user" or "staff")
            return await BuildStaffResult(personId, best.Similarity, box, cancellationToken);

        return new CdRecognitionResultDto { IsRecognized = false };
    }

    private async Task<CdRecognitionResultDto> BuildVisitorResult(
        int visitorId, double similarity, DetectedFace? box, CancellationToken ct)
    {
        var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId, ct);
        if (visitor == null)
            return new CdRecognitionResultDto { IsRecognized = false };

        var activeInvitation = await _unitOfWork.Invitations
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.VisitorId == visitorId &&
                     i.Status == InvitationStatus.Active &&
                     !i.IsDeleted,
                ct);

        var isTempAway = false;
        if (activeInvitation != null)
        {
            isTempAway = await _unitOfWork.Repository<TemporaryLeave>()
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(tl => tl.InvitationId == activeInvitation.Id && tl.IsActive, ct);
        }

        return new CdRecognitionResultDto
        {
            IsRecognized = true,
            Similarity = similarity,
            PersonType = "Visitor",
            PersonId = visitor.Id,
            FullName = visitor.FullName,
            Company = visitor.Company,
            ProfilePhotoUrl = string.IsNullOrWhiteSpace(visitor.ProfilePhotoPath)
                ? null : _urlResolver.GetAbsoluteUrl(visitor.ProfilePhotoPath),
            IsCurrentlyInside = activeInvitation != null && !isTempAway,
            IsTempAway = isTempAway,
            ActiveInvitationId = activeInvitation?.Id,
            FaceX = box?.X, FaceY = box?.Y,
            FaceWidth = box?.Width, FaceHeight = box?.Height,
            IsVip = visitor.IsVip,
            IsBlacklisted = visitor.IsBlacklisted,
            BlacklistReason = visitor.BlacklistReason,
        };
    }

    private async Task<CdRecognitionResultDto> BuildStaffResult(
        int userId, double similarity, DetectedFace? box, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user == null)
            return new CdRecognitionResultDto { IsRecognized = false };

        var activePresence = await _unitOfWork.Repository<StaffPresence>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                sp => sp.UserId == userId && sp.CheckedOutAt == null,
                ct);

        var isTempAway = activePresence?.Status == StaffPresenceStatus.TemporarilyAbsent;

        return new CdRecognitionResultDto
        {
            IsRecognized = true,
            Similarity = similarity,
            PersonType = "Staff",
            PersonId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Department = user.Department,
            ProfilePhotoUrl = string.IsNullOrWhiteSpace(user.ProfilePhotoPath)
                ? null : _urlResolver.GetAbsoluteUrl(user.ProfilePhotoPath),
            IsCurrentlyInside = activePresence != null && !isTempAway,
            IsTempAway = isTempAway,
            StaffPresenceId = activePresence?.Id,
            FaceX = box?.X, FaceY = box?.Y,
            FaceWidth = box?.Width, FaceHeight = box?.Height,
        };
    }

    private static bool TryParseSubject(string subjectId, out string personType, out int id)
    {
        personType = string.Empty;
        id = 0;
        var parts = subjectId.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        personType = parts[0].ToLowerInvariant();
        return int.TryParse(parts[1], out id);
    }
}
