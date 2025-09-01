using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Queries.VisitorDocuments;
using VisitorManagementSystem.Api.Application.Services;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

public class GetVisitorDocumentByIdQueryHandler : IRequestHandler<GetVisitorDocumentByIdQuery, VisitorDocumentDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorDocumentByIdQueryHandler> _logger;
    private readonly IFileUploadService _fileUploadService;

    public GetVisitorDocumentByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorDocumentByIdQueryHandler> logger,
        IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    public async Task<VisitorDocumentDto?> Handle(GetVisitorDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visitor document by ID: {DocumentId}", request.Id);

            var document = await _unitOfWork.VisitorDocuments.GetByIdAsync(request.Id, cancellationToken);

            if (document == null || (!request.IncludeDeleted && document.IsDeleted))
            {
                return null;
            }

            var documentDto = _mapper.Map<VisitorDocumentDto>(document);

            // Always generate URL (so frontend doesn’t need to guess)
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                documentDto.DownloadUrl = _fileUploadService.GetVisitorDocumentUrl(document.FilePath);
            }

            return documentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visitor document by ID: {DocumentId}", request.Id);
            throw;
        }
    }
}
