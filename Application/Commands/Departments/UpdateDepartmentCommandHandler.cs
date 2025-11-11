using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Departments;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Departments;

/// <summary>
/// Handler for updating a department
/// </summary>
public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, UpdateDepartmentResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateDepartmentCommandHandler> _logger;

    public UpdateDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateDepartmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateDepartmentResult> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (request.Id <= 0)
            {
                return new UpdateDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid department ID"
                };
            }

            if (request.DepartmentData == null)
            {
                return new UpdateDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Department data is required"
                };
            }

            // Find department
            var departmentsRepo = _unitOfWork.Repository<Department>();
            var departments = await departmentsRepo.GetAsync(d => d.Id == request.Id && !d.IsDeleted);

            var department = departments.FirstOrDefault();
            if (department == null)
            {
                return new UpdateDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Department with ID {request.Id} not found"
                };
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(request.DepartmentData.Name))
            {
                if (request.DepartmentData.Name.Length < 2 || request.DepartmentData.Name.Length > 100)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Department name must be between 2 and 100 characters"
                    };
                }
                department.Name = request.DepartmentData.Name.Trim();
            }

            if (request.DepartmentData.Description != null)
            {
                if (request.DepartmentData.Description.Length > 500)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Description cannot exceed 500 characters"
                    };
                }
                department.Description = request.DepartmentData.Description.Trim();
            }

            // Update manager if provided
            if (request.DepartmentData.ManagerId.HasValue)
            {
                var usersRepo = _unitOfWork.Users;
                var manager = await usersRepo.GetByIdAsync(request.DepartmentData.ManagerId.Value);

                if (manager == null || manager.IsDeleted)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Manager with ID {request.DepartmentData.ManagerId} not found"
                    };
                }

                department.ManagerId = request.DepartmentData.ManagerId;
            }

            // Update parent department if provided
            if (request.DepartmentData.ParentDepartmentId.HasValue)
            {
                // Check for circular reference
                if (request.DepartmentData.ParentDepartmentId == request.Id)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "A department cannot be its own parent"
                    };
                }

                var parentDepartments = await departmentsRepo.GetAsync(
                    d => d.Id == request.DepartmentData.ParentDepartmentId && !d.IsDeleted);

                if (!parentDepartments.Any())
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Parent department with ID {request.DepartmentData.ParentDepartmentId} not found"
                    };
                }

                department.ParentDepartmentId = request.DepartmentData.ParentDepartmentId;
            }

            if (request.DepartmentData.Email != null)
            {
                if (request.DepartmentData.Email.Length > 256)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Email cannot exceed 256 characters"
                    };
                }
                department.Email = request.DepartmentData.Email.Trim();
            }

            if (request.DepartmentData.Phone != null)
            {
                if (request.DepartmentData.Phone.Length > 20)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Phone cannot exceed 20 characters"
                    };
                }
                department.Phone = request.DepartmentData.Phone.Trim();
            }

            if (request.DepartmentData.Location != null)
            {
                if (request.DepartmentData.Location.Length > 100)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Location cannot exceed 100 characters"
                    };
                }
                department.Location = request.DepartmentData.Location.Trim();
            }

            if (request.DepartmentData.Budget.HasValue)
            {
                if (request.DepartmentData.Budget < 0)
                {
                    return new UpdateDepartmentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Budget cannot be negative"
                    };
                }
                department.Budget = request.DepartmentData.Budget;
            }

            if (request.DepartmentData.DisplayOrder.HasValue)
                department.DisplayOrder = request.DepartmentData.DisplayOrder.Value;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Department with ID {DepartmentId} updated successfully", request.Id);

            var departmentDto = _mapper.Map<DepartmentDto>(department);

            return new UpdateDepartmentResult
            {
                IsSuccess = true,
                Department = departmentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department with ID {DepartmentId}", request.Id);
            return new UpdateDepartmentResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while updating the department"
            };
        }
    }
}
