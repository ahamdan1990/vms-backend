using MediatR;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitPurposes;

/// <summary>
/// Handler for delete visit purpose command
/// </summary>
public class DeleteVisitPurposeCommandHandler : IRequestHandler<DeleteVisitPurposeCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteVisitPurposeCommandHandler> _logger;

    public DeleteVisitPurposeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteVisitPurposeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteVisitPurposeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Deleting visit purpose {VisitPurposeId}", request.Id);

            // Get existing visit purpose
            var visitPurpose = await _unitOfWork.Repository<VisitPurpose>()
                .GetByIdAsync(request.Id, cancellationToken);

            if (visitPurpose == null)
            {
                _logger.LogWarning("Visit purpose {VisitPurposeId} not found for deletion", request.Id);
                return false;
            }

            // Check if visit purpose is being used by any invitations
            var isInUse = await _unitOfWork.Repository<Invitation>()
                .AnyAsync(i => i.VisitPurposeId == request.Id, cancellationToken);

            if (isInUse)
            {
                throw new InvalidOperationException(
                    "Cannot delete visit purpose as it is currently being used by existing invitations");
            }

            if (request.SoftDelete)
            {
                // Soft delete - mark as deleted
                visitPurpose.IsDeleted = true;
                visitPurpose.DeletedBy = request.DeletedBy;
                visitPurpose.DeletedOn = DateTime.UtcNow;
                
                _unitOfWork.Repository<VisitPurpose>().Update(visitPurpose);
            }
            else
            {
                // Hard delete - remove from database
                _unitOfWork.Repository<VisitPurpose>().Remove(visitPurpose);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted visit purpose {VisitPurposeId}: {Name} (SoftDelete: {SoftDelete})", 
                visitPurpose.Id, visitPurpose.Name, request.SoftDelete);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visit purpose {VisitPurposeId}", request.Id);
            throw;
        }
    }
}
