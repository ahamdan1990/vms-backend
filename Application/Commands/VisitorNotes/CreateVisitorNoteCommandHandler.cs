using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorNotes;

/// <summary>
/// Handler for create visitor note command
/// </summary>
public class CreateVisitorNoteCommandHandler : IRequestHandler<CreateVisitorNoteCommand, VisitorNoteDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateVisitorNoteCommandHandler> _logger;

    public CreateVisitorNoteCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateVisitorNoteCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorNoteDto> Handle(CreateVisitorNoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create visitor note command for visitor: {VisitorId}", request.VisitorId);

            // Verify visitor exists
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.VisitorId, cancellationToken);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{request.VisitorId}' not found.");
            }

            // Create visitor note entity
            var visitorNote = new VisitorNote
            {
                VisitorId = request.VisitorId,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                Category = request.Category.Trim(),
                Priority = request.Priority,
                IsFlagged = request.IsFlagged,
                IsConfidential = request.IsConfidential,
                FollowUpDate = request.FollowUpDate,
                Tags = request.Tags?.Trim()
            };

            // Set audit information
            visitorNote.SetCreatedBy(request.CreatedBy);

            // Add to repository
            await _unitOfWork.VisitorNotes.AddAsync(visitorNote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor note created successfully: {NoteId} for visitor {VisitorId} by {CreatedBy}",
                visitorNote.Id, request.VisitorId, request.CreatedBy);

            // Map to DTO and return
            var visitorNoteDto = _mapper.Map<VisitorNoteDto>(visitorNote);
            return visitorNoteDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating visitor note for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
