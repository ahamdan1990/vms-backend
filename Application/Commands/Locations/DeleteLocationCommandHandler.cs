using MediatR;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Locations;

/// <summary>
/// Handler for delete location command
/// </summary>
public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteLocationCommandHandler> _logger;

    public DeleteLocationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteLocationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Deleting location {LocationId}", request.Id);

            // Get existing location
            var location = await _unitOfWork.Repository<Location>()
                .GetByIdAsync(request.Id, cancellationToken);

            if (location == null)
            {
                _logger.LogWarning("Location {LocationId} not found for deletion", request.Id);
                return false;
            }

            // Check if location is being used by any invitations
            var isInUse = await _unitOfWork.Repository<Invitation>()
                .AnyAsync(i => i.LocationId == request.Id, cancellationToken);

            if (isInUse)
            {
                throw new InvalidOperationException(
                    "Cannot delete location as it is currently being used by existing invitations");
            }

            // Check if location has child locations
            var hasChildren = await _unitOfWork.Repository<Location>()
                .AnyAsync(l => l.ParentLocationId == request.Id, cancellationToken);

            if (hasChildren)
            {
                throw new InvalidOperationException(
                    "Cannot delete location as it has child locations. Please delete child locations first");
            }

            if (request.SoftDelete)
            {
                // Soft delete - mark as deleted
                location.IsDeleted = true;
                location.DeletedBy = request.DeletedBy;
                location.DeletedOn = DateTime.UtcNow;
                
                _unitOfWork.Repository<Location>().Update(location);
            }
            else
            {
                // Hard delete - remove from database
                _unitOfWork.Repository<Location>().Remove(location);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted location {LocationId}: {Name} (SoftDelete: {SoftDelete})", 
                location.Id, location.Name, request.SoftDelete);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {LocationId}", request.Id);
            throw;
        }
    }
}
