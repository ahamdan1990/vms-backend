using MediatR;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Configuration;

namespace VisitorManagementSystem.Api.Application.Commands.FaceEvents;

public class AssignFaceEventToPersonCommandHandler
    : IRequestHandler<AssignFaceEventToPersonCommand, AssignFaceEventToPersonResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFaceTemplateEnrollmentService _enrollmentService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AssignFaceEventToPersonCommandHandler> _logger;

    public AssignFaceEventToPersonCommandHandler(
        IUnitOfWork unitOfWork,
        IFaceTemplateEnrollmentService enrollmentService,
        IWebHostEnvironment environment,
        ILogger<AssignFaceEventToPersonCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _enrollmentService = enrollmentService;
        _environment = environment;
        _logger = logger;
    }

    public async Task<AssignFaceEventToPersonResult> Handle(
        AssignFaceEventToPersonCommand request,
        CancellationToken cancellationToken)
    {
        var faceEvent = await _unitOfWork.FaceEvents.GetByIdAsync(request.FaceEventId, cancellationToken);
        if (faceEvent == null)
            return Fail($"Face event {request.FaceEventId} not found.");

        var normalizedType = request.PersonType.Equals("Staff", StringComparison.OrdinalIgnoreCase)
            ? "Staff"
            : "Visitor";

        // Verify the person exists
        if (normalizedType == "Visitor")
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.PersonId, cancellationToken);
            if (visitor == null)
                return Fail($"Visitor {request.PersonId} not found.");
        }
        else
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.PersonId, cancellationToken);
            if (user == null)
                return Fail($"User {request.PersonId} not found.");
        }

        var subjectId = normalizedType == "Staff"
            ? $"user:{request.PersonId}"
            : $"visitor:{request.PersonId}";

        // Update the face event
        faceEvent.IsKnown = true;
        faceEvent.PersonType = normalizedType;
        faceEvent.PersonId = request.PersonId;
        faceEvent.SubjectId = subjectId;
        faceEvent.ReviewStatus = FaceEventReviewStatus.Matched;
        faceEvent.ReviewedById = request.ReviewedById;
        faceEvent.ReviewedAt = DateTime.UtcNow;
        faceEvent.ReviewNotes = request.Notes;
        faceEvent.ExpiresAt = null; // retain indefinitely once matched

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Face event {FaceEventId} assigned to {PersonType} {PersonId} by user {ReviewedById}",
            request.FaceEventId, normalizedType, request.PersonId, request.ReviewedById);

        var result = new AssignFaceEventToPersonResult { Success = true, Message = "Face event assigned." };

        if (!request.EnrollSnapshot || string.IsNullOrEmpty(faceEvent.SnapshotPath))
            return result;

        var localPath = ResolveLocalPath(faceEvent.SnapshotPath);
        if (localPath == null || !File.Exists(localPath))
        {
            result.Message = "Face event assigned. Snapshot file not found — face not enrolled.";
            return result;
        }

        var imageBytes = await File.ReadAllBytesAsync(localPath, cancellationToken);
        var enrollment = await _enrollmentService.EnrollAdditionalFaceAsync(
            normalizedType,
            request.PersonId,
            imageBytes,
            sourcePath: faceEvent.SnapshotPath,
            source: "EventSnapshot",
            cancellationToken);

        result.TemplateId = enrollment.TemplateId;
        result.NewTemplateQuality = enrollment.NewTemplateQuality;
        result.PrimaryTemplateQuality = enrollment.PrimaryTemplateQuality;
        result.SuggestPromoteToPrimary = enrollment.SuggestPromoteToPrimary;

        if (enrollment.Success)
            result.Message = "Face event assigned and snapshot enrolled as additional template.";
        else
            result.Message = $"Face event assigned. Enrollment failed: {enrollment.Message}";

        return result;
    }

    private string? ResolveLocalPath(string storedPath)
    {
        if (Path.IsPathRooted(storedPath) && File.Exists(storedPath))
            return storedPath;

        if (Uri.TryCreate(storedPath, UriKind.Absolute, out _))
            return null;

        try
        {
            return VmsRuntimePaths.ResolveDataPath(_environment, storedPath);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static AssignFaceEventToPersonResult Fail(string message) =>
        new() { Success = false, Message = message };
}
