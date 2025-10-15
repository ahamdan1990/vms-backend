using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for camera entity operations
/// Provides optimized queries and specialized camera management functionality
/// </summary>
public class CameraRepository : BaseRepository<Camera>, ICameraRepository
{
    public CameraRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> NameExistsInLocationAsync(string name, int? locationId = null, int? excludeId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => c.Name.ToLower() == name.ToLower() && 
                                     c.LocationId == locationId && 
                                     !c.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetByLocationAsync(int locationId, bool includeInactive = false, 
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.LocationId == locationId);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.OrderBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetByTypeAsync(CameraType cameraType, bool includeInactive = false, 
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.CameraType == cameraType);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetByStatusAsync(CameraStatus status, bool includeDeleted = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.Status == status);

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetOperationalCamerasAsync(bool facialRecognitionOnly = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.IsActive && 
                                   !c.IsDeleted && 
                                   c.Status == CameraStatus.Active);

        if (facialRecognitionOnly)
        {
            query = query.Where(c => c.EnableFacialRecognition);
        }

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetCamerasRequiringHealthCheckAsync(int maxMinutesSinceCheck = 30, 
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-maxMinutesSinceCheck);
        
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.IsActive && 
                                   !c.IsDeleted && 
                                   (c.LastHealthCheck == null || c.LastHealthCheck < cutoffTime));

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.LastHealthCheck ?? DateTime.MinValue)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetCamerasWithHighFailureCountAsync(int minFailureCount = 5, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.IsActive && 
                                   !c.IsDeleted && 
                                   c.FailureCount >= minFailureCount);

        return await query.OrderByDescending(c => c.FailureCount)
                         .ThenBy(c => c.Priority)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetByPriorityRangeAsync(int minPriority = 1, int maxPriority = 10, 
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.Priority >= minPriority && 
                                   c.Priority <= maxPriority && 
                                   !c.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> SearchCamerasAsync(string searchTerm, CameraType? cameraType = null, 
        int? locationId = null, bool includeInactive = false, bool includeDeleted = false, 
        CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.Name.ToLower().Contains(lowerSearchTerm) ||
                                   (c.Description != null && c.Description.ToLower().Contains(lowerSearchTerm)) ||
                                   (c.Manufacturer != null && c.Manufacturer.ToLower().Contains(lowerSearchTerm)) ||
                                   (c.Model != null && c.Model.ToLower().Contains(lowerSearchTerm)) ||
                                   (c.SerialNumber != null && c.SerialNumber.ToLower().Contains(lowerSearchTerm)));

        if (cameraType.HasValue)
        {
            query = query.Where(c => c.CameraType == cameraType.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(c => c.LocationId == locationId.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<CameraType, int>> GetCameraStatsByTypeAsync(bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => !c.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.GroupBy(c => c.CameraType)
                         .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<Dictionary<CameraStatus, int>> GetCameraStatsByStatusAsync(bool includeDeleted = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.GroupBy(c => c.Status)
                         .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<List<Camera>> GetCamerasOfflineTooLongAsync(int maxHoursSinceOnline = 24, 
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-maxHoursSinceOnline);
        
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.IsActive && 
                                   !c.IsDeleted && 
                                   (c.LastOnlineTime == null || c.LastOnlineTime < cutoffTime) &&
                                   c.Status != CameraStatus.Maintenance);

        return await query.OrderBy(c => c.LastOnlineTime ?? DateTime.MinValue)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetByManufacturerAsync(string manufacturer, bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.Manufacturer != null && 
                                   c.Manufacturer.ToLower() == manufacturer.ToLower() && 
                                   !c.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<List<Camera>> GetCamerasWithFacialRecognitionAsync(bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Location)
                         .Where(c => c.EnableFacialRecognition && !c.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Priority)
                         .ThenBy(c => c.Name)
                         .ToListAsync(cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> cameraIds, CameraStatus newStatus, 
        string? errorMessage = null, int? userId = null, CancellationToken cancellationToken = default)
    {
        var cameras = await _dbSet.Where(c => cameraIds.Contains(c.Id))
                                 .ToListAsync(cancellationToken);

        var updateCount = 0;
        foreach (var camera in cameras)
        {
            camera.UpdateStatus(newStatus, errorMessage, userId);
            updateCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return updateCount;
    }

    public async Task<int> GetCameraCountByLocationAsync(int locationId, bool includeInactive = false, 
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => c.LocationId == locationId);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.CountAsync(cancellationToken);
    }
}