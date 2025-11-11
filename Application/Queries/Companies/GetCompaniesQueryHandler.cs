using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Companies;

/// <summary>
/// Handler for getting companies with pagination and filtering
/// </summary>
public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, GetCompaniesResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompaniesQueryHandler> _logger;

    public GetCompaniesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCompaniesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetCompaniesResult> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate pagination parameters
            if (request.PageNumber < 1)
                request.PageNumber = 1;
            if (request.PageSize < 1 || request.PageSize > 100)
                request.PageSize = 10;

            // Build filter predicate
            Expression<Func<Company, bool>> filter = c => !c.IsDeleted;

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLowerInvariant();
                filter = c => !c.IsDeleted &&
                    (c.Name.ToLower().Contains(searchTerm) ||
                     c.Code.ToLower().Contains(searchTerm));
            }

            // Apply verification filter
            if (request.IsVerified.HasValue)
            {
                var isVerified = request.IsVerified.Value;
                filter = c => !c.IsDeleted && c.IsVerified == isVerified;
            }

            // Get companies repository
            var companiesRepo = _unitOfWork.Repository<Company>();

            // Get total count
            var totalCount = await companiesRepo.CountAsync(filter);

            if (totalCount == 0)
            {
                return new GetCompaniesResult
                {
                    IsSuccess = true,
                    Companies = new List<CompanyDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }

            // Get all companies matching the filter
            var allCompanies = await companiesRepo.GetAsync(filter);

            // Apply sorting and pagination in memory
            var sortedQuery = GetOrderByExpression(request.SortBy, request.SortDirection)(allCompanies.AsQueryable());
            var companies = sortedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var companyDtos = _mapper.Map<List<CompanyDto>>(companies);

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            _logger.LogInformation(
                "Retrieved {Count} companies (Page {PageNumber}/{TotalPages})",
                companies.Count,
                request.PageNumber,
                totalPages);

            return new GetCompaniesResult
            {
                IsSuccess = true,
                Companies = companyDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies");
            return new GetCompaniesResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while retrieving companies"
            };
        }
    }

    /// <summary>
    /// Gets the order by expression based on sort field and direction
    /// </summary>
    private Func<IQueryable<Company>, IOrderedQueryable<Company>> GetOrderByExpression(
        string sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection?.ToLowerInvariant() == "desc";

        return sortBy?.ToLowerInvariant() switch
        {
            "code" => q => isDescending
                ? q.OrderByDescending(c => c.Code)
                : q.OrderBy(c => c.Code),
            "createdon" => q => isDescending
                ? q.OrderByDescending(c => c.CreatedOn)
                : q.OrderBy(c => c.CreatedOn),
            "visitorcount" => q => isDescending
                ? q.OrderByDescending(c => c.VisitorCount)
                : q.OrderBy(c => c.VisitorCount),
            _ => q => isDescending
                ? q.OrderByDescending(c => c.Name)
                : q.OrderBy(c => c.Name)
        };
    }
}
