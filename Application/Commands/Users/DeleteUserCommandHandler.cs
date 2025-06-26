using MediatR;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for delete user command
    /// </summary>
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IUnitOfWork unitOfWork,
            IAuthService authService,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing delete user command for user: {UserId}", request.Id);

                // Get existing user
                var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent user: {UserId}", request.Id);
                    return false;
                }

                // Prevent self-deletion
                if (user.Id == request.DeletedBy)
                {
                    _logger.LogWarning("User {UserId} attempted to delete themselves", request.Id);
                    throw new InvalidOperationException("You cannot delete your own account.");
                }

                // Check if user has active sessions and revoke them
                var activeTokens = await _unitOfWork.RefreshTokens.GetValidTokensByUserIdAsync(user.Id, cancellationToken);
                if (activeTokens.Any())
                {
                    _logger.LogInformation("Revoking {Count} active tokens for user: {UserId}", activeTokens.Count, user.Id);
                    await _unitOfWork.RefreshTokens.RevokeAllTokensForUserAsync(
                        user.Id,
                        "User account deleted",
                        null,
                        cancellationToken);
                }

                if (request.HardDelete)
                {
                    // Hard delete - permanently remove from database
                    _logger.LogWarning("Performing HARD DELETE for user: {UserId} by {DeletedBy}. Reason: {Reason}",
                        request.Id, request.DeletedBy, request.Reason);

                    // Note: In a production system, you might want to:
                    // 1. Check for foreign key constraints
                    // 2. Archive related data
                    // 3. Require additional authorization for hard deletes

                    _unitOfWork.Users.Delete(user);
                }
                else
                {
                    // Soft delete - mark as deleted
                    _logger.LogInformation("Performing soft delete for user: {UserId} by {DeletedBy}. Reason: {Reason}",
                        request.Id, request.DeletedBy, request.Reason);

                    user.SoftDelete(request.DeletedBy);
                    user.UpdateSecurityStamp(); // Invalidate any cached permissions
                    _unitOfWork.Users.Update(user);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {DeleteType} deleted successfully: {UserId} ({Email}) by {DeletedBy}",
                    request.HardDelete ? "HARD" : "soft",
                    user.Id,
                    user.Email.Value,
                    request.DeletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", request.Id);
                throw;
            }
        }
    }
}
