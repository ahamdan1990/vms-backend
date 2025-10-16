using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorNotes;

/// <summary>
/// Handler for delete visitor note command
/// </summary>
public class DeleteVisitorNoteCommandHandler : IRequestHandler<DeleteVisitorNoteCommand, CommandResultDto<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteVisitorNoteCommandHandler> _logger;

    public DeleteVisitorNoteCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteVisitorNoteCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommandResultDto<bool>> Handle(DeleteVisitorNoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing delete visitor note command for ID: {NoteId}", request.Id);

            // Get existing visitor note
            var visitorNote = await _unitOfWork.VisitorNotes.GetByIdAsync(request.Id, cancellationToken);
            if (visitorNote == null)
            {
                throw new InvalidOperationException($"Visitor note with ID '{request.Id}' not found.");
            }

            if (request.PermanentDelete)
            {
                // Permanent delete
                _unitOfWork.VisitorNotes.Delete(visitorNote);
                _logger.LogInformation("Visitor note permanently deleted: {NoteId} by {DeletedBy}",
                    request.Id, request.DeletedBy);
            }
            else
            {
                // Soft delete
                visitorNote.SoftDelete(request.DeletedBy);
                _unitOfWork.VisitorNotes.Update(visitorNote);
                _logger.LogInformation("Visitor note soft deleted: {NoteId} by {DeletedBy}",
                    request.Id, request.DeletedBy);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CommandResultDto<bool>.Success(true, "Visitor note deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor note: {NoteId}", request.Id);
            throw;
        }
    }
}
