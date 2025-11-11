using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Departments;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Departments;

/// <summary>
/// Handler for creating a new department
/// </summary>
public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, CreateDepartmentResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDepartmentCommandHandler> _logger;

    public CreateDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateDepartmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreateDepartmentResult> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (request.DepartmentData == null)
            {
                return new CreateDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Department data is required"
                };
            }

            // Validate required fields
            var errors = ValidateDepartmentInput(request.DepartmentData);
            if (errors.Count > 0)
            {
                _logger.LogWarning("Department creation validation failed");
                return new CreateDepartmentResult
                {
                    IsSuccess = false,
                    Errors = errors
                };
            }

            // Check if department code already exists
            var departmentsRepo = _unitOfWork.Repository<Department>();
            var existingDepartments = await departmentsRepo.GetAsync(
                d => d.Code == request.DepartmentData.Code.ToUpperInvariant() && !d.IsDeleted);

            if (existingDepartments.Any())
            {
                _logger.LogWarning("Department with code {Code} already exists", request.DepartmentData.Code);
                return new CreateDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Department with code '{request.DepartmentData.Code}' already exists"
                };
            }

            // Validate parent department if specified
            if (request.DepartmentData.ParentDepartmentId.HasValue)
            {
                var parentDepartments = await departmentsRepo.GetAsync(
                    d => d.Id == request.DepartmentData.ParentDepartmentId && !d.IsDeleted);

                if (!parentDepartments.Any())
                {
                    _logger.LogWarning(
                        "Parent department with ID {ParentDepartmentId} not found",
                        request.DepartmentData.ParentDepartmentId);

                    return new CreateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Parent department with ID {request.DepartmentData.ParentDepartmentId} not found"
                    };
                }
            }

            // Validate manager if specified
            if (request.DepartmentData.ManagerId.HasValue)
            {
                var usersRepo = _unitOfWork.Users;
                var manager = await usersRepo.GetByIdAsync(request.DepartmentData.ManagerId.Value);

                if (manager == null || manager.IsDeleted)
                {
                    _logger.LogWarning(
                        "Manager with ID {ManagerId} not found",
                        request.DepartmentData.ManagerId);

                    return new CreateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Manager with ID {request.DepartmentData.ManagerId} not found"
                    };
                }
            }

            // Create new department
            var department = new Department
            {
                Name = request.DepartmentData.Name.Trim(),
                Code = request.DepartmentData.Code.ToUpperInvariant().Trim(),
                Description = request.DepartmentData.Description?.Trim(),
                ManagerId = request.DepartmentData.ManagerId,
                ParentDepartmentId = request.DepartmentData.ParentDepartmentId,
                Email = request.DepartmentData.Email?.Trim(),
                Phone = request.DepartmentData.Phone?.Trim(),
                Location = request.DepartmentData.Location?.Trim(),
                Budget = request.DepartmentData.Budget,
                DisplayOrder = request.DepartmentData.DisplayOrder,
                IsActive = true,
                IsDeleted = false,
                CreatedOn = DateTime.UtcNow
            };

            await departmentsRepo.AddAsync(department, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Department created: {DepartmentName} with ID {DepartmentId}",
                department.Name, department.Id);

            var departmentDto = _mapper.Map<DepartmentDto>(department);

            return new CreateDepartmentResult
            {
                IsSuccess = true,
                Department = departmentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return new CreateDepartmentResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while creating the department"
            };
        }
    }

    /// <summary>
    /// Validates department input
    /// </summary>
    private List<string> ValidateDepartmentInput(CreateDepartmentDto request)
    {
        var errors = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Department name is required");
        else if (request.Name.Trim().Length < 2)
            errors.Add("Department name must be at least 2 characters");
        else if (request.Name.Length > 100)
            errors.Add("Department name cannot exceed 100 characters");

        // Validate code
        if (string.IsNullOrWhiteSpace(request.Code))
            errors.Add("Department code is required");
        else if (request.Code.Trim().Length < 2)
            errors.Add("Department code must be at least 2 characters");
        else if (request.Code.Length > 50)
            errors.Add("Department code cannot exceed 50 characters");

        // Validate description
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 500)
            errors.Add("Department description cannot exceed 500 characters");

        // Validate email if provided
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Length > 256)
            errors.Add("Email cannot exceed 256 characters");

        // Validate phone if provided
        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Length > 20)
            errors.Add("Phone number cannot exceed 20 characters");

        // Validate location if provided
        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Length > 100)
            errors.Add("Location cannot exceed 100 characters");

        // Validate budget if provided
        if (request.Budget.HasValue && request.Budget < 0)
            errors.Add("Budget cannot be negative");

        return errors;
    }
}
