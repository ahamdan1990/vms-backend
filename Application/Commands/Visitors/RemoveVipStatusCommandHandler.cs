using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for remove VIP status command
/// </summary>
public class RemoveVipStatusCommandHandler : IRequestHandler<RemoveVipStatusCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveVipStatusCommandHandler> _logger;

    public RemoveVipStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemoveVipStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveVipStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing remove VIP status command for ID: {Id}", request.Id);

            // Get existing visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.Id, cancellationToken);
            if (visitor == null)
            {
                _logger.LogWarning("Visitor not found: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor with ID '{request.Id}' not found.");
            }

            // Check if visitor is VIP
            if (!visitor.IsVip)
            {
                _logger.LogWarning("Visitor is not VIP: {Id}", request.Id);
                return true; // Not VIP, return success
            }

            // Remove VIP status
            visitor.RemoveVipStatus(request.ModifiedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("VIP status removed from visitor: {VisitorId} by {ModifiedBy}",
                visitor.Id, request.ModifiedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing VIP status from visitor with ID: {Id}", request.Id);
            throw;
        }
    }
}
