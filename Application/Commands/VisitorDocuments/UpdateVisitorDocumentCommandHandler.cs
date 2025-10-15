using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Handler for update visitor document command
/// </summary>
public class UpdateVisitorDocumentCommandHandler : IRequestHandler<UpdateVisitorDocumentCommand, VisitorDocumentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateVisitorDocumentCommandHandler> _logger;
    private readonly IFileUploadService _fileUploadService;

    public UpdateVisitorDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateVisitorDocumentCommandHandler> logger,
        IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    public async Task<VisitorDocumentDto> Handle(UpdateVisitorDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update visitor document command for ID: {DocumentId}", request.Id);

            // Get existing visitor document
            var visitorDocument = await _unitOfWork.VisitorDocuments.GetByIdAsync(request.Id, cancellationToken);
            if (visitorDocument == null)
            {
                throw new InvalidOperationException($"Visitor document with ID '{request.Id}' not found.");
            }

            // Update properties
            visitorDocument.Title = request.Title.Trim();
            visitorDocument.Description = request.Description?.Trim();
            visitorDocument.DocumentType = request.DocumentType.Trim();
            visitorDocument.IsSensitive = request.IsSensitive;
            visitorDocument.IsRequired = request.IsRequired;
            visitorDocument.ExpiryDate = request.ExpiryDate;
            visitorDocument.Tags = request.Tags?.Trim();

            // Set audit information
            visitorDocument.UpdateModifiedBy(request.ModifiedBy);

            // Update in repository
            _unitOfWork.VisitorDocuments.Update(visitorDocument);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor document updated successfully: {DocumentId} by {ModifiedBy}",
                request.Id, request.ModifiedBy);

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
            _logger.LogError(ex, "Error updating visitor document: {DocumentId}", request.Id);
            throw;
        }
    }
}
