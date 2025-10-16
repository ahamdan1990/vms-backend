using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Handler for create visitor document command
/// </summary>
public class CreateVisitorDocumentCommandHandler : IRequestHandler<CreateVisitorDocumentCommand, VisitorDocumentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateVisitorDocumentCommandHandler> _logger;
    private readonly IFileUploadService _fileUploadService;

    public CreateVisitorDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateVisitorDocumentCommandHandler> logger,
        IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    public async Task<VisitorDocumentDto> Handle(CreateVisitorDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create visitor document command for visitor: {VisitorId}", request.VisitorId);

            // Verify visitor exists
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.VisitorId, cancellationToken);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{request.VisitorId}' not found.");
            }

            // Create visitor document entity
            var visitorDocument = new VisitorDocument
            {
                VisitorId = request.VisitorId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                DocumentType = request.DocumentType.Trim(),
                FilePath = request.FilePath.Trim(),
                OriginalFileName = request.OriginalFileName?.Trim(),
                FileSize = request.FileSize,
                MimeType = request.MimeType?.Trim(),
                IsSensitive = request.IsSensitive,
                IsRequired = request.IsRequired,
                ExpiryDate = request.ExpiryDate,
                Tags = request.Tags?.Trim()
            };

            // Set audit information
            visitorDocument.SetCreatedBy(request.CreatedBy);

            // Add to repository
            await _unitOfWork.VisitorDocuments.AddAsync(visitorDocument, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor document created successfully: {DocumentId} for visitor {VisitorId} by {CreatedBy}",
                visitorDocument.Id, request.VisitorId, request.CreatedBy);

            // Map to DTO and return
            var visitorDocumentDto = _mapper.Map<VisitorDocumentDto>(visitorDocument);
            
            // Generate download URL for consistency with query handlers
            if (!string.IsNullOrEmpty(visitorDocument.FilePath))
            {
                visitorDocumentDto.DownloadUrl = _fileUploadService.GetVisitorDocumentUrl(visitorDocument.FilePath);
            }
            
            return visitorDocumentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating visitor document for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
