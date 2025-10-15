using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorDocuments;

/// <summary>
/// Handler for get visitor documents by visitor ID query
/// </summary>
public class GetVisitorDocumentsByVisitorIdQueryHandler : IRequestHandler<GetVisitorDocumentsByVisitorIdQuery, List<VisitorDocumentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorDocumentsByVisitorIdQueryHandler> _logger;
    private readonly IFileUploadService _fileUploadService;

    public GetVisitorDocumentsByVisitorIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorDocumentsByVisitorIdQueryHandler> logger,
        IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    public async Task<List<VisitorDocumentDto>> Handle(GetVisitorDocumentsByVisitorIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visitor documents for visitor: {VisitorId}", request.VisitorId);

            List<Domain.Entities.VisitorDocument> documents;

            if (!string.IsNullOrEmpty(request.DocumentType))
            {
                // Get by visitor and document type
                documents = await _unitOfWork.VisitorDocuments.GetByVisitorAndTypeAsync(
                    request.VisitorId, request.DocumentType, cancellationToken);
            }
            else
            {
                // Get all documents for visitor
                documents = await _unitOfWork.VisitorDocuments.GetByVisitorIdAsync(
                    request.VisitorId, cancellationToken);
            }

            // Filter out deleted documents if not requested
            if (!request.IncludeDeleted)
            {
                documents = documents.Where(d => !d.IsDeleted).ToList();
            }

            var documentDtos = _mapper.Map<List<VisitorDocumentDto>>(documents);
            
            // Generate download URLs for all documents (following same pattern as GetVisitorDocumentByIdQueryHandler)
            for (int i = 0; i < documentDtos.Count; i++)
            {
                if (!string.IsNullOrEmpty(documents[i].FilePath))
                {
                    documentDtos[i].DownloadUrl = _fileUploadService.GetVisitorDocumentUrl(documents[i].FilePath);
                }
            }
            
            return documentDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visitor documents for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
