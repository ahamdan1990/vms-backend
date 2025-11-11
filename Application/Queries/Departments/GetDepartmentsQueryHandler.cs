using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Departments;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Departments;

/// <summary>
/// Handler for getting departments with pagination and filtering
/// </summary>
public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, GetDepartmentsResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDepartmentsQueryHandler> _logger;

    public GetDepartmentsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetDepartmentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetDepartmentsResult> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate pagination parameters
            if (request.PageNumber < 1)
                request.PageNumber = 1;
            if (request.PageSize < 1 || request.PageSize > 100)
                request.PageSize = 10;

            // Build filter predicate
            Expression<Func<Department, bool>> filter = d => !d.IsDeleted;

            // Apply parent department filter
            if (request.ParentDepartmentId.HasValue)
            {
                filter = d => !d.IsDeleted && d.ParentDepartmentId == request.ParentDepartmentId;
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLowerInvariant();
                filter = d => !d.IsDeleted &&
                    (d.Name.ToLower().Contains(searchTerm) ||
                     d.Code.ToLower().Contains(searchTerm));
            }

            // Get departments repository
            var departmentsRepo = _unitOfWork.Repository<Department>();

            // Get total count
            var totalCount = await departmentsRepo.CountAsync(filter);

            if (totalCount == 0)
            {
                return new GetDepartmentsResult
                {
                    IsSuccess = true,
                    Departments = new List<DepartmentDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }

            // Get all departments matching the filter
            var allDepartments = await departmentsRepo.GetAsync(filter);

            // Apply sorting and pagination in memory
            var sortedQuery = GetOrderByExpression(request.SortBy, request.SortDirection)(allDepartments.AsQueryable());
            var departments = sortedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var departmentDtos = _mapper.Map<List<DepartmentDto>>(departments);

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            _logger.LogInformation(
                "Retrieved {Count} departments (Page {PageNumber}/{TotalPages})",
                departments.Count,
                request.PageNumber,
                totalPages);

            return new GetDepartmentsResult
            {
                IsSuccess = true,
                Departments = departmentDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return new GetDepartmentsResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while retrieving departments"
            };
        }
    }

    /// <summary>
    /// Gets the order by expression based on sort field and direction
    /// </summary>
    private Func<IQueryable<Department>, IOrderedQueryable<Department>> GetOrderByExpression(
        string sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection?.ToLowerInvariant() == "desc";

        return sortBy?.ToLowerInvariant() switch
        {
            "code" => q => isDescending
                ? q.OrderByDescending(d => d.Code)
                : q.OrderBy(d => d.Code),
            "createdon" => q => isDescending
                ? q.OrderByDescending(d => d.CreatedOn)
                : q.OrderBy(d => d.CreatedOn),
            "displayorder" => q => isDescending
                ? q.OrderByDescending(d => d.DisplayOrder)
                : q.OrderBy(d => d.DisplayOrder),
            _ => q => isDescending
                ? q.OrderByDescending(d => d.Name)
                : q.OrderBy(d => d.Name)
        };
    }
}
