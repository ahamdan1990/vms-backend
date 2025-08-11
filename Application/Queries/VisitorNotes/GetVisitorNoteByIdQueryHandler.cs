using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorNotes;

/// <summary>
/// Handler for get visitor note by ID query
/// </summary>
public class GetVisitorNoteByIdQueryHandler : IRequestHandler<GetVisitorNoteByIdQuery, VisitorNoteDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorNoteByIdQueryHandler> _logger;

    public GetVisitorNoteByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorNoteByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorNoteDto?> Handle(GetVisitorNoteByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visitor note by ID: {NoteId}", request.Id);

            var note = await _unitOfWork.VisitorNotes.GetByIdAsync(request.Id, cancellationToken);
            
            // Return null if not found or deleted (when not including deleted)
            if (note == null || (!request.IncludeDeleted && note.IsDeleted))
            {
                return null;
            }

            var noteDto = _mapper.Map<VisitorNoteDto>(note);
            return noteDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visitor note by ID: {NoteId}", request.Id);
            throw;
        }
    }
}
