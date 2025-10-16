using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Users;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for activate user command
    /// </summary>
    public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, UserDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserLockoutService _lockoutService;
        private readonly IMapper _mapper;
        private readonly ILogger<ActivateUserCommandHandler> _logger;

        public ActivateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IUserLockoutService lockoutService,
            IMapper mapper,
            ILogger<ActivateUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _lockoutService = lockoutService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing activate user command for user: {UserId}", request.Id);

                // Get existing user
                var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to activate non-existent user: {UserId}", request.Id);
                    throw new InvalidOperationException($"User with ID '{request.Id}' not found.");
                }

                // Prevent self-activation by inactive/suspended users
                if (user.Id == request.ActivatedBy && user.Status != UserStatus.Active)
                {
                    _logger.LogWarning("Inactive user {UserId} attempted to activate themselves", request.Id);
                    throw new InvalidOperationException("You cannot activate your own inactive account.");
                }

                var originalStatus = user.Status;

                // Activate user
                user.Status = UserStatus.Active;
                user.Activate(request.ActivatedBy);

                // Reset failed login attempts if requested
                if (request.ResetFailedAttempts)
                {
                    user.ResetFailedLoginAttempts();

                    // Also unlock account if it was locked
                    if (user.IsCurrentlyLockedOut())
                    {
                        user.UnlockAccount();
                        _logger.LogInformation("User account unlocked during activation: {UserId}", request.Id);
                    }
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User activated successfully: {UserId} by {ActivatedBy}. Status: {OriginalStatus} -> {NewStatus}. Reason: {Reason}",
                    user.Id, request.ActivatedBy, originalStatus, user.Status, request.Reason);

                // Map to DTO
                var userDto = _mapper.Map<UserDto>(user);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", request.Id);
                throw;
            }
        }
    }

}
