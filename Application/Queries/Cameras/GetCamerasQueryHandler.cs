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
/// Handler for retrieving paginated camera lists with advanced filtering and sorting
/// Optimized for performance with selective loading and efficient database queries
/// </summary>
public class GetCamerasQueryHandler : IRequestHandler<GetCamerasQuery, PagedResultDto<CameraListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCamerasQueryHandler> _logger;

    public GetCamerasQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCamerasQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResultDto<CameraListDto>> Handle(GetCamerasQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Retrieving cameras with filters - Page: {PageIndex}, Size: {PageSize}, " +
                           "SearchTerm: {SearchTerm}, CameraType: {CameraType}, Status: {Status}, " +
                           "LocationId: {LocationId}, IncludeDeleted: {IncludeDeleted}",
                request.PageIndex, request.PageSize, request.SearchTerm, request.CameraType,
                request.Status, request.LocationId, request.IncludeDeleted);

            // Build base query with necessary includes
            var query = _unitOfWork.Cameras.GetQueryable()
                .Include(c => c.Location)
                .AsQueryable();

            // Apply filters
            query = ApplyFilters(query, request);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, request.SortBy, request.SortDirection);

            // Apply pagination
            var cameras = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
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
                request.PageIndex, 
                request.PageSize);

            _logger.LogDebug("Successfully retrieved {Count} cameras out of {Total} total",
                cameras.Count, totalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cameras list: {Error}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Applies filtering conditions to the camera query
    /// </summary>
    private static IQueryable<Camera> ApplyFilters(IQueryable<Camera> query, GetCamerasQuery request)
    {
        // Deletion filter
        if (!request.IncludeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        // Search term filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower().Trim();
            query = query.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                (c.Description != null && c.Description.ToLower().Contains(searchTerm)) ||
                (c.Manufacturer != null && c.Manufacturer.ToLower().Contains(searchTerm)) ||
                (c.Model != null && c.Model.ToLower().Contains(searchTerm)));
        }

        // Camera type filter
        if (request.CameraType.HasValue)
        {
            query = query.Where(c => c.CameraType == request.CameraType.Value);
        }

        // Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        // Location filter
        if (request.LocationId.HasValue)
        {
            query = query.Where(c => c.LocationId == request.LocationId.Value);
        }

        // Active status filter
        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        // Facial recognition filter
        if (request.EnableFacialRecognition.HasValue)
        {
            query = query.Where(c => c.EnableFacialRecognition == request.EnableFacialRecognition.Value);
        }

        // Priority range filters
        if (request.MinPriority.HasValue)
        {
            query = query.Where(c => c.Priority >= request.MinPriority.Value);
        }

        if (request.MaxPriority.HasValue)
        {
            query = query.Where(c => c.Priority <= request.MaxPriority.Value);
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the camera query
    /// </summary>
    private static IQueryable<Camera> ApplySorting(IQueryable<Camera> query, string sortBy, string sortDirection)
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
            "createdon" => isDescending ? query.OrderByDescending(c => c.CreatedOn) : query.OrderBy(c => c.CreatedOn),
            "modifiedon" => isDescending 
                ? query.OrderByDescending(c => c.ModifiedOn ?? c.CreatedOn) 
                : query.OrderBy(c => c.ModifiedOn ?? c.CreatedOn),
            "lasthealthcheck" => isDescending 
                ? query.OrderByDescending(c => c.LastHealthCheck ?? DateTime.MinValue) 
                : query.OrderBy(c => c.LastHealthCheck ?? DateTime.MinValue),
            "failurecount" => isDescending ? query.OrderByDescending(c => c.FailureCount) : query.OrderBy(c => c.FailureCount),
            _ => isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
        };
    }

    /// <summary>
    /// Enhances camera list DTO with computed properties
    /// </summary>
    private static void EnhanceCameraListDto(CameraListDto dto, Camera camera)
    {
        // Set display values
        dto.CameraTypeDisplay = camera.CameraType.ToString();
        dto.StatusDisplay = GetStatusDisplayForList(camera);
        dto.DisplayName = camera.GetDisplayName();
        dto.IsOperational = camera.IsOperational();

        // Set location name
        dto.LocationName = camera.Location?.Name;

        // Calculate time-based fields
        var timeSinceHealthCheck = camera.TimeSinceLastHealthCheck();
        dto.MinutesSinceLastHealthCheck = timeSinceHealthCheck?.TotalMinutes > 0 
            ? (int)timeSinceHealthCheck.Value.TotalMinutes 
            : null;

        // Set health status
        dto.HealthStatus = GetHealthStatus(camera);
    }

    /// <summary>
    /// Gets concise status display text for list views
    /// </summary>
    private static string GetStatusDisplayForList(Camera camera)
    {
        return camera.Status switch
        {
            CameraStatus.Active => "Active",
            CameraStatus.Inactive => "Inactive",
            CameraStatus.Connecting => "Connecting",
            CameraStatus.Error => $"Error ({camera.FailureCount})",
            CameraStatus.Disconnected => "Disconnected",
            CameraStatus.Maintenance => "Maintenance",
            _ => camera.Status.ToString()
        };
    }

    /// <summary>
    /// Determines health status based on last health check and current status
    /// </summary>
    private static string GetHealthStatus(Camera camera)
    {
        if (!camera.IsActive || camera.IsDeleted)
            return "Disabled";

        if (camera.Status == CameraStatus.Active)
        {
            var timeSinceHealthCheck = camera.TimeSinceLastHealthCheck();
            if (timeSinceHealthCheck == null)
                return "Unknown";

            return timeSinceHealthCheck.Value.TotalMinutes switch
            {
                <= 5 => "Healthy",
                <= 15 => "Good",
                <= 30 => "Stale",
                _ => "Outdated"
            };
        }

        return camera.Status switch
        {
            CameraStatus.Inactive => "Inactive",
            CameraStatus.Connecting => "Connecting",
            CameraStatus.Error => "Error",
            CameraStatus.Disconnected => "Offline",
            CameraStatus.Maintenance => "Maintenance",
            _ => "Unknown"
        };
    }
}