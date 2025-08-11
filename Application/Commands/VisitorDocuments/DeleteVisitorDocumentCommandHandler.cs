using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Handler for delete visitor document command
/// </summary>
public class DeleteVisitorDocumentCommandHandler : IRequestHandler<DeleteVisitorDocumentCommand, CommandResultDto<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteVisitorDocumentCommandHandler> _logger;

    public DeleteVisitorDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteVisitorDocumentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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

            if (request.PermanentDelete)
            {
                // Permanent delete
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

            return CommandResultDto<bool>.Success(true, "Visitor document deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor document: {DocumentId}", request.Id);
            throw;
        }
    }
}
