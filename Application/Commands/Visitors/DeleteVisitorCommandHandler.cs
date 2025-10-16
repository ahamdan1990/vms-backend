using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
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

            // Get ALL invitations for this visitor to analyze them
            var allInvitations = await _unitOfWork.Invitations.GetByVisitorIdAsync(request.Id, cancellationToken);
            
            // Separate cancelled and active invitations
            var cancelledInvitations = allInvitations.Where(i => i.Status == InvitationStatus.Cancelled).ToList();
            var activeInvitations = allInvitations.Where(i => i.Status != InvitationStatus.Cancelled).ToList();

            // Check if there are active invitations that prevent deletion
            if (activeInvitations.Any())
            {
                var activeStatuses = string.Join(", ", activeInvitations.Select(i => i.Status.ToString()).Distinct());
                _logger.LogWarning("Cannot delete visitor {VisitorId} - has {Count} active invitations with statuses: {Statuses}", 
                    request.Id, activeInvitations.Count, activeStatuses);
                throw new InvalidOperationException($"Cannot delete visitor because there are {activeInvitations.Count} active invitations. " +
                    $"Active invitation statuses: {activeStatuses}. Please cancel these invitations first.");
            }

            // Delete cancelled invitations first if any exist
            if (cancelledInvitations.Any())
            {
                _logger.LogInformation("Deleting {Count} cancelled invitations for visitor {VisitorId}", 
                    cancelledInvitations.Count, request.Id);
                
                // First, delete all invitation events for these cancelled invitations
                var invitationEventRepo = _unitOfWork.Repository<InvitationEvent>();
                var cancelledInvitationIds = cancelledInvitations.Select(i => i.Id).ToList();
                
                var invitationEvents = await invitationEventRepo.GetAsync(
                    ie => cancelledInvitationIds.Contains(ie.InvitationId), 
                    cancellationToken);
                
                if (invitationEvents.Any())
                {
                    _logger.LogInformation("Deleting {Count} invitation events for cancelled invitations", 
                        invitationEvents.Count);
                    invitationEventRepo.RemoveRange(invitationEvents);
                }
                
                // Then delete the cancelled invitations
                _unitOfWork.Invitations.RemoveRange(cancelledInvitations);
            }

            // Now delete the visitor
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

            // Save all changes in a single transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted visitor {VisitorId} and {InvitationCount} cancelled invitations", 
                request.Id, cancelledInvitations.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor with ID: {Id}", request.Id);
            throw;
        }
    }
}
