using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Controllers; // For UserActivityDto
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Handler for GetUserActivityQuery
/// </summary>
public class GetUserActivityQueryHandler : IRequestHandler<GetUserActivityQuery, PagedResultDto<UserActivityDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserActivityQueryHandler> _logger;

    public GetUserActivityQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserActivityQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<UserActivityDto>> Handle(GetUserActivityQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing GetUserActivityQuery for UserId: {UserId}", request.UserId);

            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return new PagedResultDto<UserActivityDto>
                {
                    Items = new List<UserActivityDto>(),
                    TotalCount = 0,
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize
                };
            }

            var startDate = DateTime.UtcNow.AddDays(-request.Days);

            // Get audit logs (simplified - you'll need to implement proper audit log querying)
            var activities = new List<UserActivityDto>
            {
                new UserActivityDto
                {
                    Action = "Login",
                    Description = "User logged in successfully",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    IpAddress = "192.168.1.100",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    IsSuccess = true
                },
                new UserActivityDto
                {
                    Action = "Profile Update",
                    Description = "User updated their profile information",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    IpAddress = "192.168.1.100",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    IsSuccess = true
                }
            };

            // Apply pagination
            var totalCount = activities.Count;
            var pagedActivities = activities
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResultDto<UserActivityDto>
            {
                Items = pagedActivities,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetUserActivityQuery for UserId: {UserId}", request.UserId);
            throw;
        }
    }
}