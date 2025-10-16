using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

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
            _logger.LogDebug("Processing get visitor stats query");

            // Get basic statistics from repository
            var basicStats = await _unitOfWork.Visitors.GetVisitorStatisticsAsync(cancellationToken);

            // Get top companies
            var topCompanies = await _unitOfWork.Visitors.GetTopCompaniesByVisitorCountAsync(10, cancellationToken);

            // Get recent registrations (last 10)
            var recentVisitors = await _unitOfWork.Visitors.GetVisitorsByDateRangeAsync(
                DateTime.UtcNow.AddDays(-30), 
                DateTime.UtcNow, 
                cancellationToken);

            var recentRegistrations = recentVisitors
                .OrderByDescending(v => v.CreatedOn)
                .Take(10)
                .Select(v => v.GetMaskedInfo())
                .ToList();

            // Generate growth data (last 12 months)
            var growthData = await GenerateGrowthData(cancellationToken);

            // Get nationality distribution
            var nationalityDistribution = await GetNationalityDistribution(cancellationToken);

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

    private async Task<List<VisitorGrowthDto>> GenerateGrowthData(CancellationToken cancellationToken)
    {
        var growthData = new List<VisitorGrowthDto>();
        var now = DateTime.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = now.AddMonths(-i).Date.AddDays(1 - now.AddMonths(-i).Day);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            
            var monthlyVisitors = await _unitOfWork.Visitors.GetVisitorsByDateRangeAsync(
                monthStart, monthEnd, cancellationToken);

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

    private async Task<Dictionary<string, int>> GetNationalityDistribution(CancellationToken cancellationToken)
    {
        // This would typically be a database query with GROUP BY
        // For now, we'll fetch all active visitors and group in memory
        var visitors = await _unitOfWork.Visitors.GetAsync(v => 
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
