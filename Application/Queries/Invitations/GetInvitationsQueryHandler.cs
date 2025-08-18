using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for get invitations query
/// </summary>
public class GetInvitationsQueryHandler : IRequestHandler<GetInvitationsQuery, PagedResultDto<InvitationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInvitationsQueryHandler> _logger;

    public GetInvitationsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetInvitationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<InvitationDto>> Handle(GetInvitationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get invitations query with filters");

            // Use specific repository methods for common queries
            List<Invitation> allInvitations;

            if (request.PendingApprovalsOnly)
            {
                allInvitations = await _unitOfWork.Invitations.GetPendingApprovalsAsync(cancellationToken);
            }
            else if (request.ActiveOnly)
            {
                allInvitations = await _unitOfWork.Invitations.GetActiveInvitationsAsync(cancellationToken);
            }
            else if (request.ExpiredOnly)
            {
                allInvitations = await _unitOfWork.Invitations.GetExpiredInvitationsAsync(cancellationToken);
            }
            else if (request.HostId.HasValue)
            {
                allInvitations = await _unitOfWork.Invitations.GetByHostIdAsync(request.HostId.Value, cancellationToken);
            }
            else if (request.VisitorId.HasValue)
            {
                allInvitations = await _unitOfWork.Invitations.GetByVisitorIdAsync(request.VisitorId.Value, cancellationToken);
            }
            else if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                allInvitations = await _unitOfWork.Invitations.GetByDateRangeAsync(request.StartDate.Value, request.EndDate.Value, cancellationToken);
            }
            else if (request.Status.HasValue)
            {
                allInvitations = await _unitOfWork.Invitations.GetByStatusAsync(request.Status.Value, cancellationToken);
            }
            else
            {
                // Get all invitations - this would need a new repository method
                var repository = _unitOfWork.Repository<Invitation>();
                allInvitations = await repository.GetAllIncludingAsync(
                                                                        cancellationToken,
                                                                        i => i.Visitor,
                                                                        i => i.Host,
                                                                        i => i.VisitPurpose,
                                                                        i => i.Location
                                                                    );
            }

            // Apply additional filters in memory (for complex combinations)
            var filteredInvitations = ApplyAdditionalFilters(allInvitations, request);

            // Apply search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                filteredInvitations = ApplySearchInMemory(filteredInvitations, request.SearchTerm);
            }

            // Get total count
            var totalCount = filteredInvitations.Count();

            // Apply sorting
            var sortedInvitations = ApplySortingInMemory(filteredInvitations, request.SortBy, request.SortDirection);

            // Apply paging
            var pagedInvitations = sortedInvitations
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to DTOs
            var invitationDtos = _mapper.Map<List<InvitationDto>>(pagedInvitations);

            // Calculate paging info
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var result = new PagedResultDto<InvitationDto>
            {
                Items = invitationDtos,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitations");
            throw;
        }
    }

    private List<Invitation> ApplyAdditionalFilters(List<Invitation> invitations, GetInvitationsQuery request)
    {
        var filtered = invitations.AsQueryable();

        // Apply filters that weren't handled by specific repository methods
        if (request.Type.HasValue && !request.Status.HasValue)
        {
            filtered = filtered.Where(i => i.Type == request.Type.Value);
        }

        if (request.VisitPurposeId.HasValue && !request.HostId.HasValue && !request.VisitorId.HasValue)
        {
            filtered = filtered.Where(i => i.VisitPurposeId == request.VisitPurposeId.Value);
        }

        if (request.LocationId.HasValue && !request.HostId.HasValue && !request.VisitorId.HasValue)
        {
            filtered = filtered.Where(i => i.LocationId == request.LocationId.Value);
        }

        // Apply date filters if not already applied
        if (!request.StartDate.HasValue && !request.EndDate.HasValue)
        {
            if (request.StartDate.HasValue && !request.HostId.HasValue && !request.VisitorId.HasValue && !request.Status.HasValue)
            {
                filtered = filtered.Where(i => i.ScheduledStartTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue && !request.HostId.HasValue && !request.VisitorId.HasValue && !request.Status.HasValue)
            {
                filtered = filtered.Where(i => i.ScheduledStartTime <= request.EndDate.Value);
            }
        }

        // Filter by delete status
        if (!request.IncludeDeleted)
        {
            filtered = filtered.Where(i => !i.IsDeleted);
        }

        return filtered.ToList();
    }

    private List<Invitation> ApplySearchInMemory(List<Invitation> invitations, string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return invitations.Where(i =>
            i.InvitationNumber.ToLower().Contains(lowerSearchTerm) ||
            i.Subject.ToLower().Contains(lowerSearchTerm) ||
            (i.Visitor != null && i.Visitor.FirstName.ToLower().Contains(lowerSearchTerm)) ||
            (i.Visitor != null && i.Visitor.LastName.ToLower().Contains(lowerSearchTerm)) ||
            (i.Host != null && i.Host.FirstName.ToLower().Contains(lowerSearchTerm)) ||
            (i.Host != null && i.Host.LastName.ToLower().Contains(lowerSearchTerm)) ||
            (i.Message != null && i.Message.ToLower().Contains(lowerSearchTerm))
        ).ToList();
    }

    private List<Invitation> ApplySortingInMemory(List<Invitation> invitations, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "invitationumber" => isDescending 
                ? invitations.OrderByDescending(i => i.InvitationNumber).ToList()
                : invitations.OrderBy(i => i.InvitationNumber).ToList(),
            
            "subject" => isDescending 
                ? invitations.OrderByDescending(i => i.Subject).ToList()
                : invitations.OrderBy(i => i.Subject).ToList(),
            
            "status" => isDescending 
                ? invitations.OrderByDescending(i => i.Status).ToList()
                : invitations.OrderBy(i => i.Status).ToList(),
            
            "visitor" => isDescending 
                ? invitations.OrderByDescending(i => i.Visitor?.LastName).ThenByDescending(i => i.Visitor?.FirstName).ToList()
                : invitations.OrderBy(i => i.Visitor?.LastName).ThenBy(i => i.Visitor?.FirstName).ToList(),
            
            "host" => isDescending 
                ? invitations.OrderByDescending(i => i.Host?.LastName).ThenByDescending(i => i.Host?.FirstName).ToList()
                : invitations.OrderBy(i => i.Host?.LastName).ThenBy(i => i.Host?.FirstName).ToList(),
            
            "scheduledendtime" => isDescending 
                ? invitations.OrderByDescending(i => i.ScheduledEndTime).ToList()
                : invitations.OrderBy(i => i.ScheduledEndTime).ToList(),
            
            "createdon" => isDescending 
                ? invitations.OrderByDescending(i => i.CreatedOn).ToList()
                : invitations.OrderBy(i => i.CreatedOn).ToList(),
            
            "scheduledstarttime" or _ => isDescending 
                ? invitations.OrderByDescending(i => i.ScheduledStartTime).ToList()
                : invitations.OrderBy(i => i.ScheduledStartTime).ToList()
        };
    }
}
