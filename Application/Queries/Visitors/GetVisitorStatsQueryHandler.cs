using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get visitor stats query
/// </summary>
public class GetVisitorStatsQueryHandler : IRequestHandler<GetVisitorStatsQuery, VisitorStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorStatsQueryHandler> _logger;

    public GetVisitorStatsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorStatsDto> Handle(GetVisitorStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get visitor stats query for user {UserId}", request.UserId);

            // Check if user has ReadAll permission
            bool hasReadAll = request.UserPermissions.Contains(DomainPermissions.Visitor.ReadAll);

            // Get basic statistics from repository - filtered by user access if needed
            var basicStats = hasReadAll || !request.UserId.HasValue
                ? await _unitOfWork.Visitors.GetVisitorStatisticsAsync(cancellationToken)
                : await _unitOfWork.Visitors.GetVisitorStatisticsForUserAsync(request.UserId.Value, cancellationToken);

            // Get top companies - filtered by user access if needed
            var topCompanies = hasReadAll || !request.UserId.HasValue
                ? await _unitOfWork.Visitors.GetTopCompaniesByVisitorCountAsync(10, cancellationToken)
                : await _unitOfWork.Visitors.GetTopCompaniesByVisitorCountForUserAsync(request.UserId.Value, 10, cancellationToken);

            // Get recent registrations (last 10) - filtered by user access if needed
            var recentVisitors = hasReadAll || !request.UserId.HasValue
                ? await _unitOfWork.Visitors.GetVisitorsByDateRangeAsync(
                    DateTime.UtcNow.AddDays(-30),
                    DateTime.UtcNow,
                    cancellationToken)
                : await _unitOfWork.Visitors.GetVisitorsByDateRangeForUserAsync(
                    request.UserId.Value,
                    DateTime.UtcNow.AddDays(-30),
                    DateTime.UtcNow,
                    cancellationToken);

            var recentRegistrations = recentVisitors
                .OrderByDescending(v => v.CreatedOn)
                .Take(10)
                .Select(v => v.GetMaskedInfo())
                .ToList();

            // Generate growth data (last 12 months) - filtered by user access if needed
            var growthData = await GenerateGrowthData(request.UserId, hasReadAll, cancellationToken);

            // Get nationality distribution - filtered by user access if needed
            var nationalityDistribution = await GetNationalityDistribution(request.UserId, hasReadAll, cancellationToken);

            // Map to DTO
            var statsDto = new VisitorStatsDto
            {
                TotalVisitors = basicStats.TotalVisitors,
                ActiveVisitors = basicStats.ActiveVisitors,
                VipVisitors = basicStats.VipVisitors,
                BlacklistedVisitors = basicStats.BlacklistedVisitors,
                IncompleteProfiles = basicStats.IncompleteProfiles,
                VisitorsThisMonth = basicStats.VisitorsThisMonth,
                VisitorsThisYear = basicStats.VisitorsThisYear,
                AverageVisitsPerVisitor = basicStats.AverageVisitsPerVisitor,
                TopCompanies = _mapper.Map<List<CompanyVisitorCountDto>>(topCompanies),
                GrowthData = growthData,
                NationalityDistribution = nationalityDistribution,
                RecentRegistrations = _mapper.Map<List<VisitorListDto>>(recentRegistrations)
            };

            _logger.LogDebug("Generated visitor statistics: {TotalVisitors} total visitors", 
                statsDto.TotalVisitors);

            return statsDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating visitor statistics");
            throw;
        }
    }

    private async Task<List<VisitorGrowthDto>> GenerateGrowthData(int? userId, bool hasReadAll, CancellationToken cancellationToken)
    {
        var growthData = new List<VisitorGrowthDto>();
        var now = DateTime.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = now.AddMonths(-i).Date.AddDays(1 - now.AddMonths(-i).Day);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Filter by user access if needed
            var monthlyVisitors = hasReadAll || !userId.HasValue
                ? await _unitOfWork.Visitors.GetVisitorsByDateRangeAsync(
                    monthStart, monthEnd, cancellationToken)
                : await _unitOfWork.Visitors.GetVisitorsByDateRangeForUserAsync(
                    userId.Value, monthStart, monthEnd, cancellationToken);

            var periodName = monthStart.ToString("MMM yyyy");
            var newVisitors = monthlyVisitors.Count;
            var totalVisits = monthlyVisitors.Sum(v => v.VisitCount);

            // Calculate growth percentage (simplified)
            var previousCount = i == 11 ? 0 : growthData.LastOrDefault()?.NewVisitors ?? 0;
            var growthPercentage = previousCount == 0 ? 0 :
                ((double)(newVisitors - previousCount) / previousCount) * 100;

            growthData.Add(new VisitorGrowthDto
            {
                Period = periodName,
                NewVisitors = newVisitors,
                TotalVisits = totalVisits,
                GrowthPercentage = Math.Round(growthPercentage, 2)
            });
        }

        return growthData;
    }

    private async Task<Dictionary<string, int>> GetNationalityDistribution(int? userId, bool hasReadAll, CancellationToken cancellationToken)
    {
        // This would typically be a database query with GROUP BY
        // For now, we'll fetch all active visitors and group in memory
        var visitors = hasReadAll || !userId.HasValue
            ? await _unitOfWork.Visitors.GetAsync(v =>
                v.IsActive && !v.IsDeleted && !string.IsNullOrEmpty(v.Nationality),
                cancellationToken)
            : await _unitOfWork.Visitors.GetAsync(v =>
                v.VisitorAccesses.Any(va => va.UserId == userId.Value && va.IsActive) &&
                v.IsActive && !v.IsDeleted && !string.IsNullOrEmpty(v.Nationality),
                cancellationToken);

        var distribution = visitors
            .GroupBy(v => v.Nationality!)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return distribution;
    }
}
