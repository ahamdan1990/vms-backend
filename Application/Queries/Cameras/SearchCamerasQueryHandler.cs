using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Cameras;

/// <summary>
/// Handler for advanced camera search with comprehensive filtering and date range support
/// Implements complex query building and performance optimization for large datasets
/// </summary>
public class SearchCamerasQueryHandler : IRequestHandler<SearchCamerasQuery, PagedResultDto<CameraListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchCamerasQueryHandler> _logger;

    public SearchCamerasQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SearchCamerasQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResultDto<CameraListDto>> Handle(SearchCamerasQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var criteria = request.SearchCriteria;
            _logger.LogDebug("Performing advanced camera search with criteria: SearchTerm={SearchTerm}, " +
                           "CameraType={CameraType}, Status={Status}, LocationId={LocationId}",
                criteria.SearchTerm, criteria.CameraType, criteria.Status, criteria.LocationId);

            // Build base query with necessary includes
            var query = _unitOfWork.Cameras.GetQueryable()
                .Include(c => c.Location)
                .AsQueryable();

            // Apply comprehensive filters
            query = ApplySearchFilters(query, criteria);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting with advanced options
            query = ApplyAdvancedSorting(query, criteria.SortBy, criteria.SortDirection);

            // Apply pagination
            var cameras = await query
                .Skip(criteria.PageIndex * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs with computed properties
            var cameraDtos = new List<CameraListDto>();
            foreach (var camera in cameras)
            {
                var dto = _mapper.Map<CameraListDto>(camera);
                EnhanceCameraListDto(dto, camera);
                cameraDtos.Add(dto);
            }

            var result = PagedResultDto<CameraListDto>.Create(
                cameraDtos,
                totalCount,
                criteria.PageIndex,
                criteria.PageSize);

            _logger.LogDebug("Advanced search returned {Count} cameras out of {Total} total",
                cameras.Count, totalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing advanced camera search: {Error}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Applies comprehensive search filters including date ranges and advanced criteria
    /// </summary>
    private static IQueryable<Camera> ApplySearchFilters(IQueryable<Camera> query, CameraSearchDto criteria)
    {
        // Deletion filter
        if (!criteria.IncludeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        // Text search across multiple fields
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.ToLower().Trim();
            query = query.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                (c.Description != null && c.Description.ToLower().Contains(searchTerm)) ||
                (c.Manufacturer != null && c.Manufacturer.ToLower().Contains(searchTerm)) ||
                (c.Model != null && c.Model.ToLower().Contains(searchTerm)) ||
                (c.SerialNumber != null && c.SerialNumber.ToLower().Contains(searchTerm)) ||
                c.ConnectionString.ToLower().Contains(searchTerm));
        }

        // Camera type filter
        if (criteria.CameraType.HasValue)
        {
            query = query.Where(c => c.CameraType == criteria.CameraType.Value);
        }

        // Status filter
        if (criteria.Status.HasValue)
        {
            query = query.Where(c => c.Status == criteria.Status.Value);
        }

        // Location filter
        if (criteria.LocationId.HasValue)
        {
            query = query.Where(c => c.LocationId == criteria.LocationId.Value);
        }

        // Active status filter
        if (criteria.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == criteria.IsActive.Value);
        }

        // Facial recognition filter
        if (criteria.EnableFacialRecognition.HasValue)
        {
            query = query.Where(c => c.EnableFacialRecognition == criteria.EnableFacialRecognition.Value);
        }

        // Priority range filters
        if (criteria.MinPriority.HasValue)
        {
            query = query.Where(c => c.Priority >= criteria.MinPriority.Value);
        }

        if (criteria.MaxPriority.HasValue)
        {
            query = query.Where(c => c.Priority <= criteria.MaxPriority.Value);
        }

        // Failure count range filters
        if (criteria.MinFailureCount.HasValue)
        {
            query = query.Where(c => c.FailureCount >= criteria.MinFailureCount.Value);
        }

        if (criteria.MaxFailureCount.HasValue)
        {
            query = query.Where(c => c.FailureCount <= criteria.MaxFailureCount.Value);
        }

        // Manufacturer filter
        if (!string.IsNullOrWhiteSpace(criteria.Manufacturer))
        {
            var manufacturer = criteria.Manufacturer.ToLower().Trim();
            query = query.Where(c => c.Manufacturer != null && c.Manufacturer.ToLower().Contains(manufacturer));
        }

        // Model filter
        if (!string.IsNullOrWhiteSpace(criteria.Model))
        {
            var model = criteria.Model.ToLower().Trim();
            query = query.Where(c => c.Model != null && c.Model.ToLower().Contains(model));
        }

        // Date range filters
        if (criteria.CreatedFrom.HasValue)
        {
            query = query.Where(c => c.CreatedOn >= criteria.CreatedFrom.Value);
        }

        if (criteria.CreatedTo.HasValue)
        {
            query = query.Where(c => c.CreatedOn <= criteria.CreatedTo.Value.AddDays(1)); // Include full day
        }

        if (criteria.ModifiedFrom.HasValue)
        {
            query = query.Where(c => (c.ModifiedOn ?? c.CreatedOn) >= criteria.ModifiedFrom.Value);
        }

        if (criteria.ModifiedTo.HasValue)
        {
            query = query.Where(c => (c.ModifiedOn ?? c.CreatedOn) <= criteria.ModifiedTo.Value.AddDays(1));
        }

        // Last online date range filters
        if (criteria.LastOnlineFrom.HasValue)
        {
            query = query.Where(c => c.LastOnlineTime >= criteria.LastOnlineFrom.Value);
        }

        if (criteria.LastOnlineTo.HasValue)
        {
            query = query.Where(c => c.LastOnlineTime <= criteria.LastOnlineTo.Value.AddDays(1));
        }

        return query;
    }

    /// <summary>
    /// Applies advanced sorting with all available sort options
    /// </summary>
    private static IQueryable<Camera> ApplyAdvancedSorting(IQueryable<Camera> query, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "name" => isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "cameratype" => isDescending ? query.OrderByDescending(c => c.CameraType) : query.OrderBy(c => c.CameraType),
            "status" => isDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "location" => isDescending 
                ? query.OrderByDescending(c => c.Location!.Name ?? "") 
                : query.OrderBy(c => c.Location!.Name ?? ""),
            "priority" => isDescending ? query.OrderByDescending(c => c.Priority) : query.OrderBy(c => c.Priority),
            "manufacturer" => isDescending 
                ? query.OrderByDescending(c => c.Manufacturer ?? "") 
                : query.OrderBy(c => c.Manufacturer ?? ""),
            "model" => isDescending 
                ? query.OrderByDescending(c => c.Model ?? "") 
                : query.OrderBy(c => c.Model ?? ""),
            "createdon" => isDescending ? query.OrderByDescending(c => c.CreatedOn) : query.OrderBy(c => c.CreatedOn),
            "modifiedon" => isDescending 
                ? query.OrderByDescending(c => c.ModifiedOn ?? c.CreatedOn) 
                : query.OrderBy(c => c.ModifiedOn ?? c.CreatedOn),
            "lasthealthcheck" => isDescending 
                ? query.OrderByDescending(c => c.LastHealthCheck ?? DateTime.MinValue) 
                : query.OrderBy(c => c.LastHealthCheck ?? DateTime.MinValue),
            "lastonlinetime" => isDescending 
                ? query.OrderByDescending(c => c.LastOnlineTime ?? DateTime.MinValue) 
                : query.OrderBy(c => c.LastOnlineTime ?? DateTime.MinValue),
            "failurecount" => isDescending ? query.OrderByDescending(c => c.FailureCount) : query.OrderBy(c => c.FailureCount),
            _ => isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
        };
    }

    /// <summary>
    /// Enhances camera list DTO with computed properties for search results
    /// </summary>
    private static void EnhanceCameraListDto(CameraListDto dto, Camera camera)
    {
        // Set display values
        dto.CameraTypeDisplay = camera.CameraType.ToString();
        dto.StatusDisplay = GetStatusDisplayForSearch(camera);
        dto.DisplayName = camera.GetDisplayName();
        dto.IsOperational = camera.IsOperational();

        // Set location name
        dto.LocationName = camera.Location?.Name;

        // Calculate time-based fields
        var timeSinceHealthCheck = camera.TimeSinceLastHealthCheck();
        dto.MinutesSinceLastHealthCheck = timeSinceHealthCheck?.TotalMinutes > 0 
            ? (int)timeSinceHealthCheck.Value.TotalMinutes 
            : null;

        // Set comprehensive health status for search results
        dto.HealthStatus = GetComprehensiveHealthStatus(camera);
    }

    /// <summary>
    /// Gets detailed status display for search results
    /// </summary>
    private static string GetStatusDisplayForSearch(Camera camera)
    {
        var baseStatus = camera.Status.ToString();

        return camera.Status switch
        {
            CameraStatus.Active => camera.FailureCount > 0 ? $"Active (Recovered)" : "Active",
            CameraStatus.Inactive => "Inactive",
            CameraStatus.Connecting => "Connecting",
            CameraStatus.Error => $"Error (Failures: {camera.FailureCount})",
            CameraStatus.Disconnected => "Disconnected",
            CameraStatus.Maintenance => "Under Maintenance",
            _ => baseStatus
        };
    }

    /// <summary>
    /// Determines comprehensive health status for detailed search results
    /// </summary>
    private static string GetComprehensiveHealthStatus(Camera camera)
    {
        if (!camera.IsActive || camera.IsDeleted)
            return camera.IsDeleted ? "Deleted" : "Disabled";

        if (camera.Status == CameraStatus.Active)
        {
            var timeSinceHealthCheck = camera.TimeSinceLastHealthCheck();
            if (timeSinceHealthCheck == null)
                return "Never Checked";

            var healthStatus = timeSinceHealthCheck.Value.TotalMinutes switch
            {
                <= 2 => "Excellent",
                <= 5 => "Healthy",
                <= 15 => "Good",
                <= 30 => "Stale",
                <= 60 => "Outdated",
                _ => "Very Outdated"
            };

            // Add failure indicator if camera had recent issues
            if (camera.FailureCount > 0 && camera.FailureCount <= 3)
                healthStatus += " (Recovered)";
            else if (camera.FailureCount > 3)
                healthStatus += " (Unstable)";

            return healthStatus;
        }

        return camera.Status switch
        {
            CameraStatus.Inactive => "Inactive",
            CameraStatus.Connecting => "Connecting",
            CameraStatus.Error => $"Error ({camera.FailureCount} failures)",
            CameraStatus.Disconnected => "Offline",
            CameraStatus.Maintenance => "Under Maintenance",
            _ => "Unknown Status"
        };
    }
}