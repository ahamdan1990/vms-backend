using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Departments;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Departments;

/// <summary>
/// Handler for getting a single department by ID
/// </summary>
public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, GetDepartmentByIdResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDepartmentByIdQueryHandler> _logger;

    public GetDepartmentByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetDepartmentByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetDepartmentByIdResult> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Id <= 0)
            {
                return new GetDepartmentByIdResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid department ID"
                };
            }

            var departmentsRepo = _unitOfWork.Repository<Department>();
            var departments = await departmentsRepo.GetAsync(d => d.Id == request.Id && !d.IsDeleted);

            var department = departments.FirstOrDefault();

            if (department == null)
            {
                _logger.LogWarning("Department with ID {DepartmentId} not found", request.Id);
                return new GetDepartmentByIdResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Department with ID {request.Id} not found"
                };
            }

            // Get child departments if requested
            if (request.IncludeChildren)
            {
                var childDepartments = await departmentsRepo.GetAsync(d => d.ParentDepartmentId == request.Id && !d.IsDeleted);
                department.ChildDepartments = childDepartments.OrderBy(d => d.DisplayOrder).ToList();
            }

            var departmentDto = _mapper.Map<DepartmentDto>(department);

            _logger.LogInformation("Retrieved department with ID {DepartmentId}", request.Id);

            return new GetDepartmentByIdResult
            {
                IsSuccess = true,
                Department = departmentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department with ID {DepartmentId}", request.Id);
            return new GetDepartmentByIdResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while retrieving the department"
            };
        }
    }
}
