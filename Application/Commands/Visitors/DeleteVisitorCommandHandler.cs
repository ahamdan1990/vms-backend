using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for delete visitor command
/// </summary>
public class DeleteVisitorCommandHandler : IRequestHandler<DeleteVisitorCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteVisitorCommandHandler> _logger;

    public DeleteVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteVisitorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteVisitorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing delete visitor command for ID: {Id}", request.Id);

            // Get existing visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.Id, cancellationToken);
            if (visitor == null)
            {
                _logger.LogWarning("Visitor not found: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor with ID '{request.Id}' not found.");
            }

            if (request.PermanentDelete)
            {
                // Permanent delete - remove from database
                _unitOfWork.Visitors.Remove(visitor);
                _logger.LogInformation("Visitor permanently deleted: {VisitorId} by {DeletedBy}",
                    visitor.Id, request.DeletedBy);
            }
            else
            {
                // Soft delete - mark as deleted
                visitor.SoftDelete(request.DeletedBy);
                _unitOfWork.Visitors.Update(visitor);
                _logger.LogInformation("Visitor soft deleted: {VisitorId} by {DeletedBy}",
                    visitor.Id, request.DeletedBy);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor with ID: {Id}", request.Id);
            throw;
        }
    }
}
