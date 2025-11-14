using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for searching a visitor by face recognition
/// </summary>
public class SearchVisitorByPhotoQueryHandler : IRequestHandler<SearchVisitorByPhotoQuery, VisitorDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchVisitorByPhotoQueryHandler> _logger;

    public SearchVisitorByPhotoQueryHandler(
        IUnitOfWork unitOfWork,
        IFaceDetectionService faceDetectionService,
        IMapper mapper,
        ILogger<SearchVisitorByPhotoQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _faceDetectionService = faceDetectionService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorDto?> Handle(SearchVisitorByPhotoQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Photo == null || request.Photo.Length == 0)
            {
                _logger.LogWarning("No photo provided for face recognition search");
                return null;
            }

            _logger.LogDebug("Searching for visitor by face recognition, photo size: {Size} KB",
                request.Photo.Length / 1024);

            // Use RecognizeFacesAsync to find matching faces
            using var photoStream = request.Photo.OpenReadStream();
            var recognizedFaces = await _faceDetectionService.RecognizeFacesAsync(photoStream, cancellationToken);

            if (recognizedFaces == null || !recognizedFaces.Any())
            {
                _logger.LogInformation("No matching faces found in the recognition collection");
                return null;
            }

            // Get the best match (highest similarity)
            var bestMatch = recognizedFaces
                .OrderByDescending(f => f.Similarity)
                .FirstOrDefault();

            if (bestMatch == null)
            {
                return null;
            }

            _logger.LogInformation("Found matching face with similarity {Similarity:P2} for subject: {SubjectId}",
                bestMatch.Similarity, bestMatch.SubjectId);

            // The subject ID is either email or name, try to find visitor by email first
            var visitor = await _unitOfWork.Visitors.GetByEmailAsync(bestMatch.SubjectId, cancellationToken);

            if (visitor == null)
            {
                // If not found by email, try searching by name (fallback for old records)
                var nameParts = bestMatch.SubjectId.Replace("_", " ").Split(' ', 2);
                if (nameParts.Length >= 2)
                {
                    var searchResult = await _unitOfWork.Visitors.SearchVisitorsAsync(
                        $"{nameParts[0]} {nameParts[1]}",
                        pageIndex: 0,
                        pageSize: 10,
                        cancellationToken: cancellationToken);

                    visitor = searchResult.Visitors?.FirstOrDefault();
                }
            }

            if (visitor == null)
            {
                _logger.LogWarning("Face recognized as {SubjectId} but no visitor found in database",
                    bestMatch.SubjectId);
                return null;
            }

            _logger.LogInformation("Successfully found visitor {VisitorId} ({VisitorName}) by face recognition",
                visitor.Id, visitor.FullName);

            return _mapper.Map<VisitorDto>(visitor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching visitor by face recognition");
            return null;
        }
    }
}
