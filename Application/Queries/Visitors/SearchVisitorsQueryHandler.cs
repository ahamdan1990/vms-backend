using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Specifications;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for search visitors query
/// </summary>
public class SearchVisitorsQueryHandler : IRequestHandler<SearchVisitorsQuery, PagedResultDto<VisitorListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchVisitorsQueryHandler> _logger;

    public SearchVisitorsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SearchVisitorsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<VisitorListDto>> Handle(SearchVisitorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing search visitors query - Term: {SearchTerm}", request.SearchTerm);

            // Create comprehensive search specification
            var specification = new VisitorFilterSpecification(
                searchTerm: request.SearchTerm,
                company: request.Company,
                isVip: request.IsVip,
                isBlacklisted: request.IsBlacklisted,
                isActive: request.IsActive,
                nationality: request.Nationality,
                securityClearance: request.SecurityClearance,
                minVisitCount: request.MinVisitCount,
                maxVisitCount: request.MaxVisitCount,
                createdFrom: request.CreatedFrom,
                createdTo: request.CreatedTo,
                lastVisitFrom: request.LastVisitFrom,
                lastVisitTo: request.LastVisitTo,
                includeDeleted: request.IncludeDeleted,
                sortBy: request.SortBy,
                sortDirection: request.SortDirection,
                pageIndex: request.PageIndex,
                pageSize: request.PageSize
            );

            // Get paginated search results
            var (visitors, totalCount) = await _unitOfWork.Visitors.GetPagedAsync(specification, cancellationToken);

            // Map to DTOs with masked sensitive information
            var visitorDtos = visitors.Select(v => _mapper.Map<VisitorListDto>(v.GetMaskedInfo())).ToList();

            // Create paged result
            var pagedResult = PagedResultDto<VisitorListDto>.Create(
                visitorDtos,
                totalCount,
                request.PageIndex,
                request.PageSize
            );

            _logger.LogDebug("Found {Count} visitors matching search criteria out of {TotalCount}", 
                visitorDtos.Count, totalCount);

            return pagedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching visitors");
            throw;
        }
    }
}
