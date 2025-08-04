using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for UnlockUserCommand
    /// </summary>
    public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, CommandResultDto<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserLockoutService _lockoutService;
        private readonly ILogger<UnlockUserCommandHandler> _logger;

        public UnlockUserCommandHandler(
            IUnitOfWork unitOfWork,
            IUserLockoutService lockoutService,
            ILogger<UnlockUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _lockoutService = lockoutService;
            _logger = logger;
        }

        public async Task<CommandResultDto<object>> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing UnlockUserCommand for UserId: {UserId} by User: {UnlockedBy}",
                    request.UserId, request.UnlockedBy);

                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    return CommandResultDto<object>.Failure("User not found");
                }

                if (!user.IsCurrentlyLockedOut())
                {
                    return CommandResultDto<object>.Failure("User account is not currently locked");
                }

                var reason = request.Reason ?? "Administrative unlock";
                var success = await _lockoutService.UnlockUserAccountAsync(request.UserId, reason, request.UnlockedBy, cancellationToken);

                if (success)
                {
                    _logger.LogInformation("User account unlocked successfully: UserId: {UserId}, UnlockedBy: {UnlockedBy}, Reason: {Reason}",
                        request.UserId, request.UnlockedBy, reason);

                    return CommandResultDto<object>.Success("User account unlocked successfully");
                }

                return CommandResultDto<object>.Failure("Failed to unlock user account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UnlockUserCommand for UserId: {UserId}", request.UserId);
                return CommandResultDto<object>.Failure("An error occurred while unlocking the account");
            }
        }
    }
}
