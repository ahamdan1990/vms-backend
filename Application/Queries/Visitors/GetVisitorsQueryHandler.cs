using AutoMapper;
using MediatR;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Specifications;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get visitors query
/// </summary>
public class GetVisitorsQueryHandler : IRequestHandler<GetVisitorsQuery, PagedResultDto<VisitorListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorsQueryHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetVisitorsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorsQueryHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResultDto<VisitorListDto>> Handle(GetVisitorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get visitors query - Page: {PageIndex}, Size: {PageSize}",
                request.PageIndex, request.PageSize);

            // Get current user ID and permissions
            var currentUserId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            bool hasReadAll = userPermissions.Contains(DomainPermissions.Visitor.ReadAll);
            bool hasReadOwn = userPermissions.Contains(DomainPermissions.Visitor.ReadOwn) ||
                              userPermissions.Contains(DomainPermissions.Visitor.Read);

            List<VisitorListDto> visitorDtos;
            int totalCount;

            // If user has ONLY ReadOwn (not ReadAll), filter by accessible visitors
            if (hasReadOwn && !hasReadAll && currentUserId.HasValue)
            {
                _logger.LogDebug("User {UserId} has scoped visitor access - filtering by accessible visitors", currentUserId.Value);

                var (visitors, count) = await _unitOfWork.Visitors.GetAccessibleByUserAsync(
                    userId: currentUserId.Value,
                    pageIndex: request.PageIndex,
                    pageSize: request.PageSize,
                    searchTerm: request.SearchTerm,
                    company: request.Company,
                    isVip: request.IsVip,
                    isBlacklisted: request.IsBlacklisted,
                    cancellationToken);

                totalCount = count;
                visitorDtos = _mapper.Map<List<VisitorListDto>>(visitors);
            }
            else
            {
                // User has ReadAll or no scoped permission - use unrestricted query
                _logger.LogDebug("User has full visitor access - using unrestricted query");

                // Create specification for filtering
                var specification = new VisitorFilterSpecification(
                    searchTerm: request.SearchTerm,
                    company: request.Company,
                    isVip: request.IsVip,
                    isBlacklisted: request.IsBlacklisted,
                    isActive: request.IsActive,
                    includeDeleted: request.IncludeDeleted,
                    sortBy: request.SortBy,
                    sortDirection: request.SortDirection,
                    pageIndex: request.PageIndex,
                    pageSize: request.PageSize
                );

                // Get paginated results
                var (visitors, count) = await _unitOfWork.Visitors.GetPagedAsync(specification, cancellationToken);
                totalCount = count;
                visitorDtos = _mapper.Map<List<VisitorListDto>>(visitors);
            }

            // Create paged result
            var pagedResult = PagedResultDto<VisitorListDto>.Create(
                visitorDtos,
                totalCount,
                request.PageIndex,
                request.PageSize
            );

            _logger.LogDebug("Retrieved {Count} visitors out of {TotalCount}", visitorDtos.Count, totalCount);

            return pagedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visitors");
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
