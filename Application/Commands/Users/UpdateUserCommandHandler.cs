using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
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
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
                var originalRole = user.Role;

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
                user.Role = request.Role;
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

                // Update preferences
                user.UpdatePreferences(
                    timeZone: request.TimeZone,
                    language: request.Language,
                    theme: request.Theme);

                // Update security stamp if requested or if role changed
                if (request.UpdateSecurityStamp || originalRole != request.Role)
                {
                    user.UpdateSecurityStamp();
                    _logger.LogInformation("Security stamp updated for user: {UserId} due to {Reason}",
                        request.Id, originalRole != request.Role ? "role change" : "manual request");
                }

                // Set audit information
                user.UpdateModifiedBy(request.ModifiedBy);

                // Update user in repository
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User updated successfully: {UserId} by {ModifiedBy}. Email: {OriginalEmail} -> {NewEmail}, Role: {OriginalRole} -> {NewRole}",
                    user.Id, request.ModifiedBy, originalEmail, user.Email.Value, originalRole, user.Role);

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
