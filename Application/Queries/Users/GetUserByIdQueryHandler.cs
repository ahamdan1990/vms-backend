using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Queries.Users;
using VisitorManagementSystem.Api.Domain.Enums;

using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing GetUserByIdQuery for UserId: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return null;
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email.Value,
                PhoneNumber = user.PhoneNumber?.Value,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                Department = user.Department,
                JobTitle = user.JobTitle,
                EmployeeId = user.EmployeeId,
                ProfilePhotoUrl = user.ProfilePhotoPath,
                TimeZone = user.TimeZone,
                Language = user.Language,
                Theme = user.Theme,
                LastLoginDate = user.LastLoginDate,
                CreatedOn = user.CreatedOn,
                IsActive = user.Status == UserStatus.Active,
                IsLockedOut = user.IsCurrentlyLockedOut(),
                FailedLoginAttempts = user.FailedLoginAttempts,
                MustChangePassword = user.MustChangePassword,
                PasswordChangedDate = user.PasswordChangedDate
            };

            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetUserByIdQuery for UserId: {UserId}", request.UserId);
            throw;
        }
    }
}