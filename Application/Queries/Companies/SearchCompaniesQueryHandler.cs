using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Companies;

/// <summary>
/// Handler for searching companies
/// </summary>
public class SearchCompaniesQueryHandler : IRequestHandler<SearchCompaniesQuery, SearchCompaniesResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchCompaniesQueryHandler> _logger;

    public SearchCompaniesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SearchCompaniesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SearchCompaniesResult> Handle(SearchCompaniesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return new SearchCompaniesResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Search term is required"
                };
            }

            if (request.MaxResults < 1 || request.MaxResults > 100)
                request.MaxResults = 20;

            var searchTerm = request.SearchTerm.Trim().ToLowerInvariant();

            // Build filter predicate based on search field
            Expression<Func<Company, bool>> filter = request.SearchField?.ToLowerInvariant() switch
            {
                "code" => c => !c.IsDeleted && (c.Code ?? "").ToLower().Contains(searchTerm),
                "industry" => c => !c.IsDeleted && (c.Industry ?? "").ToLower().Contains(searchTerm),
                "contactpersonname" => c => !c.IsDeleted && (c.ContactPersonName ?? "").ToLower().Contains(searchTerm),
                _ => c => !c.IsDeleted && (
                    (c.Name ?? "").ToLower().Contains(searchTerm) ||
                    (c.Code ?? "").ToLower().Contains(searchTerm) ||
                    (c.Industry ?? "").ToLower().Contains(searchTerm) ||
                    (c.ContactPersonName ?? "").ToLower().Contains(searchTerm))
            };

            var companiesRepo = _unitOfWork.Repository<Company>();

            // Get search results
            var allCompanies = await companiesRepo.GetAsync(filter);

            // Apply sorting and limit in memory
            var companies = allCompanies
                .OrderBy(c => c.Name)
                .Take(request.MaxResults)
                .ToList();

            if (!companies.Any())
            {
                _logger.LogInformation("No companies found matching search term: {SearchTerm}", request.SearchTerm);
                return new SearchCompaniesResult
                {
                    IsSuccess = true,
                    Companies = new List<CompanyDto>(),
                    ResultCount = 0
                };
            }

            var companyDtos = _mapper.Map<List<CompanyDto>>(companies);

            _logger.LogInformation(
                "Found {Count} companies matching search term: {SearchTerm}",
                companies.Count,
                request.SearchTerm);

            return new SearchCompaniesResult
            {
                IsSuccess = true,
                Companies = companyDtos,
                ResultCount = companies.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching companies with term: {SearchTerm}", request.SearchTerm);
            return new SearchCompaniesResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while searching companies"
            };
        }
    }
}
