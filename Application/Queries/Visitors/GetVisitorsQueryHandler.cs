using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Specifications;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get visitors query
/// </summary>
public class GetVisitorsQueryHandler : IRequestHandler<GetVisitorsQuery, PagedResultDto<VisitorListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorsQueryHandler> _logger;

    public GetVisitorsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<VisitorListDto>> Handle(GetVisitorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get visitors query - Page: {PageIndex}, Size: {PageSize}", 
                request.PageIndex, request.PageSize);

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
            var (visitors, totalCount) = await _unitOfWork.Visitors.GetPagedAsync(specification, cancellationToken);

            // Map to DTOs
            var visitorDtos = _mapper.Map<List<VisitorListDto>>(visitors);

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
}
