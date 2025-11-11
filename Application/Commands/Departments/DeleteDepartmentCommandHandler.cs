using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Departments;

/// <summary>
/// Handler for deleting a department (soft delete)
/// </summary>
public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, DeleteDepartmentResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteDepartmentCommandHandler> _logger;

    public DeleteDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteDepartmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeleteDepartmentResult> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (request.Id <= 0)
            {
                return new DeleteDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid department ID"
                };
            }

            // Find department
            var departmentsRepo = _unitOfWork.Repository<Department>();
            var departments = await departmentsRepo.GetAsync(d => d.Id == request.Id && !d.IsDeleted);

            var department = departments.FirstOrDefault();
            if (department == null)
            {
                return new DeleteDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Department with ID {request.Id} not found"
                };
            }

            // Check if department has child departments
            var childDepartments = await departmentsRepo.GetAsync(d => d.ParentDepartmentId == request.Id && !d.IsDeleted);

            if (childDepartments.Any())
            {
                _logger.LogWarning(
                    "Cannot delete department {DepartmentId} because it has {ChildCount} child departments",
                    request.Id,
                    childDepartments.Count);

                return new DeleteDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot delete department with {childDepartments.Count} child departments. Please delete or reassign child departments first."
                };
            }

            // Check if department has active users
            var userCount = department.Users?.Where(u => !u.IsDeleted).Count() ?? 0;
            if (userCount > 0)
            {
                _logger.LogWarning(
                    "Cannot delete department {DepartmentId} because it has {UserCount} active users",
                    request.Id,
                    userCount);

                return new DeleteDepartmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot delete department with {userCount} active users. Please reassign users first."
                };
            }

            // Soft delete the department
            department.IsDeleted = true;
            department.DeletedOn = DateTime.UtcNow;
            // Note: DeletedBy should be set by the DbContext interceptor using the current user context

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Department with ID {DepartmentId} deleted successfully. Reason: {Reason}",
                request.Id,
                request.Reason ?? "Not provided");

            return new DeleteDepartmentResult
            {
                IsSuccess = true,
                Message = $"Department '{department.Name}' has been successfully deleted"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department with ID {DepartmentId}", request.Id);
            return new DeleteDepartmentResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while deleting the department"
            };
        }
    }
}
