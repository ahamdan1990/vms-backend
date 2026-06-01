using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for delete visitor command
/// </summary>
public class DeleteVisitorCommandHandler : IRequestHandler<DeleteVisitorCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteVisitorCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteVisitorCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
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

            // SECURITY: Check delete permissions
            var userPermissions = GetCurrentUserPermissions();
            bool hasReadAll = userPermissions.Contains(DomainPermissions.Visitor.ReadAll);
            bool hasDelete = userPermissions.Contains(DomainPermissions.Visitor.Delete);

            // If user has Delete permission but NOT ReadAll, verify they have access to this specific visitor
            // (ReadAll is used as a proxy for admin-level access since there's no DeleteAll permission)
            if (hasDelete && !hasReadAll)
            {
                var hasAccess = await _unitOfWork.VisitorAccess.HasAccessAsync(
                    request.DeletedBy,
                    visitor.Id,
                    cancellationToken);

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} attempted to delete visitor {VisitorId} without access permission",
                        request.DeletedBy, visitor.Id);
                    throw new UnauthorizedAccessException("You do not have permission to delete this visitor.");
                }
            }

            // Get ALL invitations for this visitor to analyze them
            var allInvitations = await _unitOfWork.Invitations.GetByVisitorIdAsync(request.Id, cancellationToken);

            // Only pending/in-progress states block deletion — the visitor still has active work in the system
            var blockingStatuses = new[]
            {
                InvitationStatus.Draft,
                InvitationStatus.Submitted,
                InvitationStatus.UnderReview,
                InvitationStatus.Approved,
                InvitationStatus.Active
            };

            var blockingInvitations = allInvitations.Where(i => blockingStatuses.Contains(i.Status)).ToList();
            var terminalInvitations = allInvitations.Where(i => !blockingStatuses.Contains(i.Status)).ToList();

            // Block deletion only when there are pending or in-progress invitations
            if (blockingInvitations.Any())
            {
                var blockingStatusStr = string.Join(", ", blockingInvitations.Select(i => i.Status.ToString()).Distinct());
                _logger.LogWarning("Cannot delete visitor {VisitorId} - has {Count} pending invitations with statuses: {Statuses}",
                    request.Id, blockingInvitations.Count, blockingStatusStr);
                throw new InvalidOperationException(
                    $"Cannot delete visitor because there are {blockingInvitations.Count} pending invitations " +
                    $"with statuses: {blockingStatusStr}. Please cancel these invitations first.");
            }

            // Soft-delete all terminal invitations (Completed, Rejected, Expired, Cancelled) alongside the visitor
            if (terminalInvitations.Any())
            {
                _logger.LogInformation("Soft-deleting {Count} terminal invitations for visitor {VisitorId}",
                    terminalInvitations.Count, request.Id);

                foreach (var invitation in terminalInvitations)
                {
                    invitation.SoftDelete(request.DeletedBy);
                    _unitOfWork.Invitations.Update(invitation);
                }
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

            _logger.LogInformation("Successfully deleted visitor {VisitorId} and soft-deleted {InvitationCount} terminal invitations",
                request.Id, terminalInvitations.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor with ID: {Id}", request.Id);
            throw;
        }
    }

    private List<string> GetCurrentUserPermissions()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }
}
