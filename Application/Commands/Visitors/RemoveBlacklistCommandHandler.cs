using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for remove blacklist command
/// </summary>
public class RemoveBlacklistCommandHandler : IRequestHandler<RemoveBlacklistCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveBlacklistCommandHandler> _logger;

    public RemoveBlacklistCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemoveBlacklistCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveBlacklistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing remove blacklist command for ID: {Id}", request.Id);

            // Get existing visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.Id, cancellationToken);
            if (visitor == null)
            {
                _logger.LogWarning("Visitor not found: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor with ID '{request.Id}' not found.");
            }

            // Check if visitor is blacklisted
            if (!visitor.IsBlacklisted)
            {
                _logger.LogWarning("Visitor is not blacklisted: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor is not blacklisted.");
            }

            // Remove blacklist status
            visitor.RemoveBlacklist(request.ModifiedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Blacklist removed from visitor: {VisitorId} by {ModifiedBy}",
                visitor.Id, request.ModifiedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing blacklist from visitor with ID: {Id}", request.Id);
            throw;
        }
    }
}
