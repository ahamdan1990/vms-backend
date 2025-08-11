using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorNotes;

/// <summary>
/// Handler for get visitor notes by visitor ID query
/// </summary>
public class GetVisitorNotesByVisitorIdQueryHandler : IRequestHandler<GetVisitorNotesByVisitorIdQuery, List<VisitorNoteDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorNotesByVisitorIdQueryHandler> _logger;

    public GetVisitorNotesByVisitorIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorNotesByVisitorIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<VisitorNoteDto>> Handle(GetVisitorNotesByVisitorIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visitor notes for visitor: {VisitorId}", request.VisitorId);

            List<Domain.Entities.VisitorNote> notes;

            if (!string.IsNullOrEmpty(request.Category))
            {
                // This would require extending the repository with category filtering
                var allNotes = await _unitOfWork.VisitorNotes.GetByVisitorIdAsync(request.VisitorId, cancellationToken);
                notes = allNotes.Where(n => n.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                notes = await _unitOfWork.VisitorNotes.GetByVisitorIdAsync(request.VisitorId, cancellationToken);
            }

            // Apply filters
            if (!request.IncludeDeleted)
            {
                notes = notes.Where(n => !n.IsDeleted).ToList();
            }

            if (request.IsFlagged.HasValue)
            {
                notes = notes.Where(n => n.IsFlagged == request.IsFlagged.Value).ToList();
            }

            if (request.IsConfidential.HasValue)
            {
                notes = notes.Where(n => n.IsConfidential == request.IsConfidential.Value).ToList();
            }

            var noteDtos = _mapper.Map<List<VisitorNoteDto>>(notes);
            return noteDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visitor notes for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
