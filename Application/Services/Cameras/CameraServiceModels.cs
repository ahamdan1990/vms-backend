using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Result of a camera connection test operation
/// Provides detailed information about connection attempts and failures
/// </summary>
public class CameraConnectionTestResult
{
    /// <summary>
    /// Whether the connection test was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Connection test status
    /// </summary>
    public CameraStatus Status { get; set; }

    /// <summary>
    /// Error message if connection failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Connection response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Timestamp when the test was performed
    /// </summary>
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional details about the connection test
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Creates a successful connection test result
    /// </summary>
    public static CameraConnectionTestResult Success(int responseTimeMs = 0)
    {
        return new CameraConnectionTestResult
        {
            IsSuccess = true,
            Status = CameraStatus.Active,
            ResponseTimeMs = responseTimeMs
        };
    }

    /// <summary>
    /// Creates a failed connection test result
    /// </summary>
    public static CameraConnectionTestResult Failure(string errorMessage, CameraStatus status = CameraStatus.Error)
    {
        return new CameraConnectionTestResult
        {
            IsSuccess = false,
            Status = status,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Information about a camera stream
/// Contains streaming status and configuration details
/// </summary>
public class CameraStreamInfo
{
    /// <summary>
    /// Camera ID
    /// </summary>
    public int CameraId { get; set; }

    /// <summary>
    /// Whether the camera is currently streaming
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    /// Stream URL (if applicable)
    /// </summary>
    public string? StreamUrl { get; set; }

    /// <summary>
    /// Current frame rate
    /// </summary>
    public int? CurrentFrameRate { get; set; }

    /// <summary>
    /// Current resolution width
    /// </summary>
    public int? CurrentWidth { get; set; }

    /// <summary>
    /// Current resolution height
    /// </summary>
    public int? CurrentHeight { get; set; }

    /// <summary>
    /// Number of active connections to this stream
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// When streaming started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Total streaming duration in seconds
    /// </summary>
    public double DurationSeconds => StartedAt.HasValue 
        ? (DateTime.UtcNow - StartedAt.Value).TotalSeconds 
        : 0;

    /// <summary>
    /// Stream quality indicator (0-100)
    /// </summary>
    public int? QualityScore { get; set; }

    /// <summary>
    /// Additional stream metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of a camera health check operation
/// Provides comprehensive status information about camera health
/// </summary>
public class CameraHealthCheckResult
{
    /// <summary>
    /// Camera ID
    /// </summary>
    public int CameraId { get; set; }

    /// <summary>
    /// Camera name for reference
    /// </summary>
    public string CameraName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the health check passed
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Current camera status
    /// </summary>
    public CameraStatus Status { get; set; }

    /// <summary>
    /// Previous camera status (for comparison)
    /// </summary>
    public CameraStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Health check error message (if any)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Response time for the health check in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current failure count
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Whether this check recovered from a previous failure
    /// </summary>
    public bool IsRecovery => PreviousStatus == CameraStatus.Error && Status == CameraStatus.Active;

    /// <summary>
    /// Whether this check indicates a new failure
    /// </summary>
    public bool IsNewFailure => PreviousStatus == CameraStatus.Active && Status == CameraStatus.Error;

    /// <summary>
    /// Health score (0-100, higher is better)
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Additional health check details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Calculates health score based on various factors
    /// </summary>
    public void CalculateHealthScore()
    {
        var score = 100;

        // Reduce score based on status
        score -= Status switch
        {
            CameraStatus.Active => 0,
            CameraStatus.Connecting => 20,
            CameraStatus.Disconnected => 40,
            CameraStatus.Maintenance => 30,
            CameraStatus.Error => 60,
            CameraStatus.Inactive => 80,
            _ => 50
        };

        // Reduce score based on failure count
        score -= Math.Min(FailureCount * 5, 30);

        // Reduce score based on response time
        if (ResponseTimeMs > 1000) score -= 10;
        else if (ResponseTimeMs > 5000) score -= 20;
        else if (ResponseTimeMs > 10000) score -= 30;

        // Ensure score is within valid range
        HealthScore = Math.Max(0, Math.Min(100, score));
        IsHealthy = HealthScore >= 70;
    }

    /// <summary>
    /// Creates a successful health check result
    /// </summary>
    public static CameraHealthCheckResult Healthy(int cameraId, string cameraName, int responseTimeMs = 0)
    {
        var result = new CameraHealthCheckResult
        {
            CameraId = cameraId,
            CameraName = cameraName,
            IsHealthy = true,
            Status = CameraStatus.Active,
            ResponseTimeMs = responseTimeMs
        };
        result.CalculateHealthScore();
        return result;
    }

    /// <summary>
    /// Creates a failed health check result
    /// </summary>
    public static CameraHealthCheckResult Unhealthy(int cameraId, string cameraName, 
        string errorMessage, CameraStatus status = CameraStatus.Error, int failureCount = 1)
    {
        var result = new CameraHealthCheckResult
        {
            CameraId = cameraId,
            CameraName = cameraName,
            IsHealthy = false,
            Status = status,
            ErrorMessage = errorMessage,
            FailureCount = failureCount
        };
        result.CalculateHealthScore();
        return result;
    }
}