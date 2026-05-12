namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Preferred face recognition engine for camera processing.
/// </summary>
public enum FaceRecognitionEngine
{
    /// <summary>
    /// Luxand FaceSDK is the primary engine.
    /// </summary>
    Luxand = 1,

    /// <summary>
    /// CompreFace is used directly.
    /// </summary>
    CompreFace = 2,

    /// <summary>
    /// Luxand is primary and CompreFace is used only as a configured fallback.
    /// </summary>
    Hybrid = 3
}
