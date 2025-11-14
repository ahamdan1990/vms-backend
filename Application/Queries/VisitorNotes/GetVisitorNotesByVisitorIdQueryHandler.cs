using AutoMapper;
using MediatR;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorNotes;

/// <summary>
/// Handler for get visitor notes by visitor ID query
/// </summary>
public class GetVisitorNotesByVisitorIdQueryHandler : IRequestHandler<GetVisitorNotesByVisitorIdQuery, List<VisitorNoteDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorNotesByVisitorIdQueryHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetVisitorNotesByVisitorIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorNotesByVisitorIdQueryHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<VisitorNoteDto>> Handle(GetVisitorNotesByVisitorIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visitor notes for visitor: {VisitorId}", request.VisitorId);

            // SECURITY: Check if user has access to this visitor's notes
            var currentUserId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            bool hasReadAll = userPermissions.Contains(DomainPermissions.Visitor.ReadAll);
            bool hasReadOwn = userPermissions.Contains(DomainPermissions.Visitor.ReadOwn) ||
                              userPermissions.Contains(DomainPermissions.Visitor.Read);

            // If user has ONLY ReadOwn (not ReadAll), verify they have access to this visitor
            if (hasReadOwn && !hasReadAll && currentUserId.HasValue)
            {
                var hasAccess = await _unitOfWork.VisitorAccess.HasAccessAsync(
                    currentUserId.Value,
                    request.VisitorId,
                    cancellationToken);

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} attempted to access notes for visitor {VisitorId} without permission",
                        currentUserId.Value, request.VisitorId);
                    return new List<VisitorNoteDto>(); // Return empty list instead of throwing
                }

                _logger.LogDebug("User {UserId} has scoped access to visitor {VisitorId}",
                    currentUserId.Value, request.VisitorId);
            }

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

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private List<string> GetCurrentUserPermissions()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }
}
