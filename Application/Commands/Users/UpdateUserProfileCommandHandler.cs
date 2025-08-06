using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Handler for update user profile command (self-service)
/// </summary>
public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;
    public UpdateUserProfileCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserProfileDto> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update user profile command for user: {UserId}", request.UserId);

            // Get existing user
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempt to update profile for non-existent user: {UserId}", request.UserId);
                throw new InvalidOperationException($"User with ID '{request.UserId}' not found.");
            }

            var originalEmail = user.Email.Value;
            // Validate email uniqueness if email is being changed
            if (!user.Email.Value.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _unitOfWork.Users.EmailExistsAsync(request.Email, request.UserId, cancellationToken))
                {
                    _logger.LogWarning("Attempt to update user {UserId} with existing email: {Email}", request.UserId, request.Email);
                    throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
                }
            }

            // Validate employee ID uniqueness if employee ID is being changed
            if (!string.Equals(user.EmployeeId, request.EmployeeId, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(request.EmployeeId) &&
                    await _unitOfWork.Users.EmployeeIdExistsAsync(request.EmployeeId, request.UserId, cancellationToken))
                {
                    _logger.LogWarning("Attempt to update user {UserId} with existing employee ID: {EmployeeId}",
                        request.UserId, request.EmployeeId);
                    throw new InvalidOperationException($"A user with employee ID '{request.EmployeeId}' already exists.");
                }
            }
            // Update basic profile properties
            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = new Email(request.Email.Trim().ToLowerInvariant());
            user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
            user.Department = request.Department?.Trim();
            user.JobTitle = request.JobTitle?.Trim();
            user.EmployeeId = request.EmployeeId?.Trim();

            // Update phone number
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = new PhoneNumber(request.PhoneNumber);
            }
            else
            {
                user.PhoneNumber = null;
            }

            // Update address if provided
            if (!string.IsNullOrEmpty(request.Street1) && !string.IsNullOrEmpty(request.City) &&
                !string.IsNullOrEmpty(request.State) && !string.IsNullOrEmpty(request.PostalCode) &&
                !string.IsNullOrEmpty(request.Country))
            {
                user.Address = new Address(
                    street1: request.Street1.Trim(),
                    city: request.City.Trim(),
                    state: request.State.Trim(),
                    postalCode: request.PostalCode.Trim(),
                    country: request.Country.Trim(),
                    street2: request.Street2?.Trim(),
                    addressType: "Home"
                );
            }
            else if (string.IsNullOrEmpty(request.Street1))
            {
                // Clear address if street1 is empty
                user.Address = null;
            }
            // Set audit information
            user.UpdateModifiedOn();

            // Update user in repository
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User profile updated successfully: {UserId}. Email: {OriginalEmail} -> {NewEmail}",
                user.Id, originalEmail, user.Email.Value);

            // Map to DTO
            var userProfileDto = _mapper.Map<UserProfileDto>(user);
            return userProfileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile: {UserId}", request.UserId);
            throw;
        }
    }
}
