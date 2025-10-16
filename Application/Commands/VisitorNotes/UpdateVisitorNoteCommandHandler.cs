using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorNotes;

/// <summary>
/// Handler for update visitor note command
/// </summary>
public class UpdateVisitorNoteCommandHandler : IRequestHandler<UpdateVisitorNoteCommand, VisitorNoteDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateVisitorNoteCommandHandler> _logger;

    public UpdateVisitorNoteCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateVisitorNoteCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorNoteDto> Handle(UpdateVisitorNoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update visitor note command for ID: {NoteId}", request.Id);

            // Get existing visitor note
            var visitorNote = await _unitOfWork.VisitorNotes.GetByIdAsync(request.Id, cancellationToken);
            if (visitorNote == null)
            {
                throw new InvalidOperationException($"Visitor note with ID '{request.Id}' not found.");
            }

            // Update properties
            visitorNote.Title = request.Title.Trim();
            visitorNote.Content = request.Content.Trim();
            visitorNote.Category = request.Category.Trim();
            visitorNote.Priority = request.Priority;
            visitorNote.IsFlagged = request.IsFlagged;
            visitorNote.IsConfidential = request.IsConfidential;
            visitorNote.FollowUpDate = request.FollowUpDate;
            visitorNote.Tags = request.Tags?.Trim();

            // Set audit information
            visitorNote.UpdateModifiedBy(request.ModifiedBy);

            // Update in repository
            _unitOfWork.VisitorNotes.Update(visitorNote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor note updated successfully: {NoteId} by {ModifiedBy}",
                request.Id, request.ModifiedBy);

            // Map to DTO and return
            var visitorNoteDto = _mapper.Map<VisitorNoteDto>(visitorNote);
            return visitorNoteDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visitor note: {NoteId}", request.Id);
            throw;
        }
    }
}
