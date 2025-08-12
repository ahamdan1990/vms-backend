using MediatR;
using System.Text.Json;
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

            var summary = await _unitOfWork.Users.GetUserActivitySummaryAsync(request.UserId, request.Days, cancellationToken);

            var auditLogs = await _unitOfWork.Users.GetUserAuditLogsAsync(request.UserId, request.Days, request.PageIndex, request.PageSize, cancellationToken);

            // Map audit logs to UserActivityDto
            var activities = auditLogs.Items.Select(al => new UserActivityDto
            {
                Action = al.Action, // Fixed: use actual action instead of dictionary toString
                Description = al.Description ?? $"{al.Action}",
                Timestamp = al.Timestamp,
                IpAddress = al.IpAddress,
                UserAgent = al.UserAgent,
                IsSuccess = al.IsSuccess,
                // Summary fields (not used for individual activity records but required by DTO)
                LoginCount = summary.LoginCount,
                LastLogin = summary.LastLogin,
                FailedLoginAttempts = summary.FailedLoginAttempts,
                LastFailedLogin = summary.LastFailedLogin,
                InvitationsCreated = summary.InvitationsCreated,
                PasswordChanges = summary.PasswordChanges,
                ActivityByType = summary.ActivityByType,
                RecentActions = summary.RecentActions
            }).ToList();

            // Use totalCount from auditLogs.TotalCount
            return new PagedResultDto<UserActivityDto>
            {
                Items = activities,
                TotalCount = auditLogs.TotalCount,
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