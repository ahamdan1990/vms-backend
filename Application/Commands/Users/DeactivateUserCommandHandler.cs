using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for deactivate user command
    /// </summary>
    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, UserDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly ILogger<DeactivateUserCommandHandler> _logger;

        public DeactivateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IAuthService authService,
            IMapper mapper,
            ILogger<DeactivateUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing deactivate user command for user: {UserId}", request.Id);

                // Get existing user
                var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to deactivate non-existent user: {UserId}", request.Id);
                    throw new InvalidOperationException($"User with ID '{request.Id}' not found.");
                }

                // Prevent self-deactivation
                if (user.Id == request.DeactivatedBy)
                {
                    _logger.LogWarning("User {UserId} attempted to deactivate themselves", request.Id);
                    throw new InvalidOperationException("You cannot deactivate your own account.");
                }

                var originalStatus = user.Status;

                // Deactivate user
                user.Status = UserStatus.Inactive;
                user.Deactivate(request.DeactivatedBy);
                user.UpdateSecurityStamp(); // Invalidate existing tokens

                // Revoke all sessions if requested
                if (request.RevokeAllSessions)
                {
                    var revokedTokens = await _unitOfWork.RefreshTokens.RevokeAllTokensForUserAsync(
                        user.Id,
                        "Account deactivated",
                        null,
                        cancellationToken);

                    _logger.LogInformation("Revoked {Count} active sessions for deactivated user: {UserId}",
                        revokedTokens, user.Id);
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User deactivated successfully: {UserId} by {DeactivatedBy}. Status: {OriginalStatus} -> {NewStatus}. Reason: {Reason}",
                    user.Id, request.DeactivatedBy, originalStatus, user.Status, request.Reason);

                // Map to DTO
                var userDto = _mapper.Map<UserDto>(user);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", request.Id);
                throw;
            }
        }
    }
}
