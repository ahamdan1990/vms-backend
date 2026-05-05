using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for update user command
    /// </summary>
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPermissionService permissionService,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing update user command for user: {UserId}", request.Id);

                // Get existing user
                var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to update non-existent user: {UserId}", request.Id);
                    throw new InvalidOperationException($"User with ID '{request.Id}' not found.");
                }

                var originalEmail = user.Email.Value;
                var originalRoleId = user.RoleId;

                // Validate email uniqueness if email is being changed
                if (!user.Email.Value.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _unitOfWork.Users.EmailExistsAsync(request.Email, request.Id, cancellationToken))
                    {
                        _logger.LogWarning("Attempt to update user {UserId} with existing email: {Email}", request.Id, request.Email);
                        throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
                    }
                }

                // Validate employee ID uniqueness if employee ID is being changed
                if (!string.Equals(user.EmployeeId, request.EmployeeId, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(request.EmployeeId) &&
                        await _unitOfWork.Users.EmployeeIdExistsAsync(request.EmployeeId, request.Id, cancellationToken))
                    {
                        _logger.LogWarning("Attempt to update user {UserId} with existing employee ID: {EmployeeId}",
                            request.Id, request.EmployeeId);
                        throw new InvalidOperationException($"A user with employee ID '{request.EmployeeId}' already exists.");
                    }
                }

                // Update user properties
                user.FirstName = request.FirstName.Trim();
                user.LastName = request.LastName.Trim();
                user.Email = new Email(request.Email.Trim().ToLowerInvariant());
                user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();

                // Resolve role from database (supports all roles, not just the deprecated enum)
                var dbRole = await _unitOfWork.Roles.GetByNameAsync(request.Role, cancellationToken);
                if (dbRole == null)
                    throw new InvalidOperationException($"Role '{request.Role}' not found.");

                user.RoleId = dbRole.Id;

                // Keep the deprecated enum in sync for legacy code paths that still read it
                if (Enum.TryParse<UserRole>(request.Role, out var parsedRole))
                    user.Role = parsedRole;

                var roleChanged = user.RoleId != originalRoleId;

                user.Status = request.Status;
                user.Department = request.Department?.Trim();
                user.JobTitle = request.JobTitle?.Trim();
                user.EmployeeId = request.EmployeeId?.Trim();

                // Update enhanced phone number
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var fullPhoneNumber = !string.IsNullOrEmpty(request.PhoneCountryCode)
                        ? $"+{request.PhoneCountryCode}{request.PhoneNumber}"
                        : request.PhoneNumber;

                    user.PhoneNumber = new PhoneNumber(fullPhoneNumber, request.PhoneCountryCode);
                }
                else
                {
                    user.PhoneNumber = null;
                }

                // Update enhanced address
                if (!string.IsNullOrEmpty(request.Street1) || !string.IsNullOrEmpty(request.City))
                {
                    user.Address = new Address(
                        request.Street1,
                        request.City,
                        request.State,
                        request.PostalCode,
                        request.Country,
                        request.Street2,
                        request.AddressType ?? "Home",
                        request.Latitude,
                        request.Longitude
                    );
                }
                else
                {
                    user.Address = null;
                }

                // Update per-host approval override (admin-only intent; UI shows only for Staff/Host roles)
                user.RequiresApprovalOverride = request.RequiresApprovalOverride;

                // Update preferences
                user.UpdatePreferences(
                    timeZone: request.TimeZone,
                    language: request.Language,
                    theme: request.Theme);

                // Update security stamp if requested or if role changed
                if (request.UpdateSecurityStamp || roleChanged)
                {
                    user.UpdateSecurityStamp();
                    _logger.LogInformation("Security stamp updated for user: {UserId} due to {Reason}",
                        request.Id, roleChanged ? "role change" : "manual request");
                }

                // Set audit information
                user.UpdateModifiedBy(request.ModifiedBy);

                // Update user in repository
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Invalidate permission cache so the next login/request picks up the new role's permissions
                if (roleChanged)
                {
                    _permissionService.InvalidateUserPermissionCache(user.Id);
                    _logger.LogInformation("Permission cache invalidated for user {UserId} due to role change", user.Id);
                }

                _logger.LogInformation("User updated successfully: {UserId} by {ModifiedBy}. Email: {OriginalEmail} -> {NewEmail}, RoleId: {OriginalRoleId} -> {NewRoleId}",
                    user.Id, request.ModifiedBy, originalEmail, user.Email.Value, originalRoleId, user.RoleId);

                // Map to DTO
                var userDto = _mapper.Map<UserDto>(user);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", request.Id);
                throw;
            }
        }
    }
}
