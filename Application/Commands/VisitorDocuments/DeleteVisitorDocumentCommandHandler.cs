using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Handler for delete visitor document command
/// </summary>
public class DeleteVisitorDocumentCommandHandler : IRequestHandler<DeleteVisitorDocumentCommand, CommandResultDto<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteVisitorDocumentCommandHandler> _logger;
    private readonly IFileUploadService _fileUploadService;

    public DeleteVisitorDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteVisitorDocumentCommandHandler> logger,
        IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    public async Task<CommandResultDto<bool>> Handle(DeleteVisitorDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing delete visitor document command for ID: {DocumentId}", request.Id);

            // Get existing visitor document
            var visitorDocument = await _unitOfWork.VisitorDocuments.GetByIdAsync(request.Id, cancellationToken);
            if (visitorDocument == null)
            {
                throw new InvalidOperationException($"Visitor document with ID '{request.Id}' not found.");
            }

            // Store file path for cleanup
            var filePath = visitorDocument.FilePath;

            if (request.PermanentDelete)
            {
                // Permanent delete - remove physical file
                _unitOfWork.VisitorDocuments.Delete(visitorDocument);
                _logger.LogInformation("Visitor document permanently deleted: {DocumentId} by {DeletedBy}",
                    request.Id, request.DeletedBy);
            }
            else
            {
                // Soft delete
                visitorDocument.SoftDelete(request.DeletedBy);
                _unitOfWork.VisitorDocuments.Update(visitorDocument);
                _logger.LogInformation("Visitor document soft deleted: {DocumentId} by {DeletedBy}",
                    request.Id, request.DeletedBy);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Clean up physical file for permanent deletes
            if (request.PermanentDelete && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    await _fileUploadService.RemoveVisitorDocumentAsync(filePath);
                    _logger.LogInformation("Physical file removed for permanently deleted document: {FilePath}", filePath);
                }
                catch (Exception fileEx)
                {
                    _logger.LogWarning(fileEx, "Failed to remove physical file for document {DocumentId}: {FilePath}", 
                        request.Id, filePath);
                    // Don't fail the entire operation if file cleanup fails
                }
            }

            return CommandResultDto<bool>.Success(true, "Visitor document deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor document: {DocumentId}", request.Id);
            throw;
        }
    }
}
