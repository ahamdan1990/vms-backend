using AutoMapper;
using MediatR;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get visitor by ID query
/// </summary>
public class GetVisitorByIdQueryHandler : IRequestHandler<GetVisitorByIdQuery, VisitorDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorByIdQueryHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetVisitorByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorByIdQueryHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<VisitorDto?> Handle(GetVisitorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get visitor by ID query: {Id}", request.Id);

            // Get visitor with related data
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(
                request.Id,
                v => v.EmergencyContacts,
                v => v.Documents,
                v => v.VisitorNotes,
                v => v.CreatedByUser!,
                v => v.ModifiedByUser!,
                v => v.BlacklistedByUser!,
                v => v.CompanyEntity!,
                v => v.PreferredLocation!,
                v => v.DefaultVisitPurpose!
            );

            if (visitor == null)
            {
                _logger.LogDebug("Visitor not found: {Id}", request.Id);
                return null;
            }

            // Check if deleted and not including deleted
            if (visitor.IsDeleted && !request.IncludeDeleted)
            {
                _logger.LogDebug("Visitor is deleted and IncludeDeleted is false: {Id}", request.Id);
                return null;
            }

            // SECURITY: Check access permissions
            var currentUserId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            bool hasReadAll = userPermissions.Contains(DomainPermissions.Visitor.ReadAll);
            bool hasReadOwn = userPermissions.Contains(DomainPermissions.Visitor.ReadOwn) ||
                              userPermissions.Contains(DomainPermissions.Visitor.Read);

            // If user has ONLY ReadOwn (not ReadAll), verify they have access to this specific visitor
            if (hasReadOwn && !hasReadAll && currentUserId.HasValue)
            {
                var hasAccess = await _unitOfWork.VisitorAccess.HasAccessAsync(
                    currentUserId.Value,
                    visitor.Id,
                    cancellationToken);

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} attempted to access visitor {VisitorId} without permission",
                        currentUserId.Value, visitor.Id);
                    return null;
                }

                _logger.LogDebug("User {UserId} has scoped access to visitor {VisitorId}",
                    currentUserId.Value, visitor.Id);
            }

            // Map to DTO
            var visitorDto = _mapper.Map<VisitorDto>(visitor);

            _logger.LogDebug("Retrieved visitor: {Id} - {FullName}", visitor.Id, visitor.FullName);

            return visitorDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visitor with ID: {Id}", request.Id);
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
