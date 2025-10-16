using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for blacklist visitor command
/// </summary>
public class BlacklistVisitorCommandHandler : IRequestHandler<BlacklistVisitorCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BlacklistVisitorCommandHandler> _logger;

    public BlacklistVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<BlacklistVisitorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(BlacklistVisitorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing blacklist visitor command for ID: {Id}", request.Id);

            // Get existing visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.Id, cancellationToken);
            if (visitor == null)
            {
                _logger.LogWarning("Visitor not found: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor with ID '{request.Id}' not found.");
            }

            // Check if visitor is already blacklisted
            if (visitor.IsBlacklisted)
            {
                _logger.LogWarning("Visitor is already blacklisted: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor is already blacklisted.");
            }

            // Validate reason
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new ArgumentException("Blacklist reason is required.");
            }

            // Blacklist the visitor
            visitor.Blacklist(request.Reason.Trim(), request.BlacklistedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor blacklisted: {VisitorId} by {BlacklistedBy} - Reason: {Reason}",
                visitor.Id, request.BlacklistedBy, request.Reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting visitor with ID: {Id}", request.Id);
            throw;
        }
    }
}
