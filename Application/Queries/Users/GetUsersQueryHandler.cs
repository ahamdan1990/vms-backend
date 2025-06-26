using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Queries.Users;
using VisitorManagementSystem.Api.Domain.Enums;

using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Handler for GetUsersQuery
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResultDto<UserListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUsersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing GetUsersQuery with PageIndex: {PageIndex}, PageSize: {PageSize}",
                request.PageIndex, request.PageSize);

            var (users, totalCount) = await _unitOfWork.Users.GetPaginatedAsync(
                request.PageIndex,
                request.PageSize,
                request.SortBy,
                request.SortDescending,
                cancellationToken);

            // Apply additional filtering if specified
            var filteredUsers = users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLowerInvariant();
                filteredUsers = filteredUsers.Where(u =>
                    u.FirstName.ToLowerInvariant().Contains(searchTerm) ||
                    u.LastName.ToLowerInvariant().Contains(searchTerm) ||
                    u.Email.Value.ToLowerInvariant().Contains(searchTerm) ||
                    (u.EmployeeId != null && u.EmployeeId.ToLowerInvariant().Contains(searchTerm)));
            }

            if (request.Role.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.Role == request.Role.Value);
            }

            if (request.Status.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.Status == request.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Department))
            {
                filteredUsers = filteredUsers.Where(u => u.Department == request.Department);
            }

            if (request.CreatedAfter.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.CreatedOn >= request.CreatedAfter.Value);
            }

            if (request.CreatedBefore.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.CreatedOn <= request.CreatedBefore.Value);
            }

            if (!request.IncludeInactive)
            {
                filteredUsers = filteredUsers.Where(u => u.Status == UserStatus.Active);
            }

            var userList = filteredUsers.Select(u => new UserListDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email.Value,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                Department = u.Department,
                LastLoginDate = u.LastLoginDate,
                CreatedOn = u.CreatedOn,
                IsActive = u.Status == UserStatus.Active,
                IsLockedOut = u.IsCurrentlyLockedOut()
            }).ToList();

            var actualTotalCount = filteredUsers.Count();

            return new PagedResultDto<UserListDto>
            {
                Items = userList,
                TotalCount = actualTotalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetUsersQuery");
            throw;
        }
    }
}