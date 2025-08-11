using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for mark as VIP command
/// </summary>
public class MarkAsVipCommandHandler : IRequestHandler<MarkAsVipCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkAsVipCommandHandler> _logger;

    public MarkAsVipCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkAsVipCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(MarkAsVipCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing mark as VIP command for ID: {Id}", request.Id);

            // Get existing visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.Id, cancellationToken);
            if (visitor == null)
            {
                _logger.LogWarning("Visitor not found: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor with ID '{request.Id}' not found.");
            }

            // Check if visitor is already VIP
            if (visitor.IsVip)
            {
                _logger.LogWarning("Visitor is already VIP: {Id}", request.Id);
                return true; // Already VIP, return success
            }

            // Mark as VIP
            visitor.MarkAsVip(request.ModifiedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor marked as VIP: {VisitorId} by {ModifiedBy}",
                visitor.Id, request.ModifiedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking visitor as VIP with ID: {Id}", request.Id);
            throw;
        }
    }
}
