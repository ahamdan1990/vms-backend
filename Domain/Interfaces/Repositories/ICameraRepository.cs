using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for camera operations
/// Provides specialized methods for camera entity management and querying
/// </summary>
public interface ICameraRepository : IGenericRepository<Camera>
{
    /// <summary>
    /// Checks if a camera with the specified name exists within the same location
    /// </summary>
    /// <param name="name">Camera name</param>
    /// <param name="locationId">Location ID (null for no location)</param>
    /// <param name="excludeId">ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if camera name exists in the location</returns>
    Task<bool> NameExistsInLocationAsync(string name, int? locationId = null, int? excludeId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras by location ID
    /// </summary>
    /// <param name="locationId">Location ID</param>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="includeDeleted">Whether to include deleted cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras in the location</returns>
    Task<List<Camera>> GetByLocationAsync(int locationId, bool includeInactive = false, 
        bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras by type
    /// </summary>
    /// <param name="cameraType">Camera type</param>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="includeDeleted">Whether to include deleted cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras of the specified type</returns>
    Task<List<Camera>> GetByTypeAsync(CameraType cameraType, bool includeInactive = false, 
        bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras by status
    /// </summary>
    /// <param name="status">Camera status</param>
    /// <param name="includeDeleted">Whether to include deleted cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras with the specified status</returns>
    Task<List<Camera>> GetByStatusAsync(CameraStatus status, bool includeDeleted = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active cameras (operational and streaming-capable)
    /// </summary>
    /// <param name="facialRecognitionOnly">Only include cameras with facial recognition enabled</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operational cameras</returns>
    Task<List<Camera>> GetOperationalCamerasAsync(bool facialRecognitionOnly = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras that require health check (haven't been checked recently)
    /// </summary>
    /// <param name="maxMinutesSinceCheck">Maximum minutes since last health check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras needing health check</returns>
    Task<List<Camera>> GetCamerasRequiringHealthCheckAsync(int maxMinutesSinceCheck = 30, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras with high failure count
    /// </summary>
    /// <param name="minFailureCount">Minimum failure count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras with high failure count</returns>
    Task<List<Camera>> GetCamerasWithHighFailureCountAsync(int minFailureCount = 5, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras by priority range
    /// </summary>
    /// <param name="minPriority">Minimum priority (1 = highest)</param>
    /// <param name="maxPriority">Maximum priority</param>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras within priority range</returns>
    Task<List<Camera>> GetByPriorityRangeAsync(int minPriority = 1, int maxPriority = 10, 
        bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches cameras by name, description, manufacturer, model, or serial number
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="cameraType">Optional camera type filter</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="includeDeleted">Whether to include deleted cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching cameras</returns>
    Task<List<Camera>> SearchCamerasAsync(string searchTerm, CameraType? cameraType = null, 
        int? locationId = null, bool includeInactive = false, bool includeDeleted = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets camera statistics grouped by type
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of camera counts by type</returns>
    Task<Dictionary<CameraType, int>> GetCameraStatsByTypeAsync(bool includeInactive = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets camera statistics grouped by status
    /// </summary>
    /// <param name="includeDeleted">Whether to include deleted cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of camera counts by status</returns>
    Task<Dictionary<CameraStatus, int>> GetCameraStatsByStatusAsync(bool includeDeleted = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras that haven't been online for a specified time
    /// </summary>
    /// <param name="maxHoursSinceOnline">Maximum hours since last online</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras that have been offline too long</returns>
    Task<List<Camera>> GetCamerasOfflineTooLongAsync(int maxHoursSinceOnline = 24, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras by manufacturer
    /// </summary>
    /// <param name="manufacturer">Manufacturer name</param>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras from the specified manufacturer</returns>
    Task<List<Camera>> GetByManufacturerAsync(string manufacturer, bool includeInactive = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cameras with facial recognition enabled
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cameras with facial recognition enabled</returns>
    Task<List<Camera>> GetCamerasWithFacialRecognitionAsync(bool includeInactive = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update camera statuses
    /// </summary>
    /// <param name="cameraIds">Camera IDs to update</param>
    /// <param name="newStatus">New status to set</param>
    /// <param name="errorMessage">Optional error message</param>
    /// <param name="userId">User ID for audit trail</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of cameras updated</returns>
    Task<int> BulkUpdateStatusAsync(IEnumerable<int> cameraIds, CameraStatus newStatus, 
        string? errorMessage = null, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets camera count by location
    /// </summary>
    /// <param name="locationId">Location ID</param>
    /// <param name="includeInactive">Whether to include inactive cameras</param>
    /// <param name="includeDeleted">Whether to include deleted cameras</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of cameras in the location</returns>
    Task<int> GetCameraCountByLocationAsync(int locationId, bool includeInactive = false, 
        bool includeDeleted = false, CancellationToken cancellationToken = default);
}