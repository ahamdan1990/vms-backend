namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// A face tracked by Luxand FaceSDK across video frames.
/// </summary>
public sealed class TrackedFace
{
    public long FaceId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[]? Template { get; init; }

    /// <summary>
    /// In-plane face rotation in degrees from FSDK TFacePosition.angle.
    /// Populated when DetermineRotationAngle tracker parameter is true.
    /// </summary>
    public float? Roll { get; init; }

    /// <summary>
    /// Left-right head rotation (Pan) in degrees.
    /// Populated when DetectAngles tracker parameter is true.
    /// </summary>
    public float? Yaw { get; init; }

    /// <summary>
    /// Up-down head rotation (Tilt) in degrees.
    /// Populated when DetectAngles tracker parameter is true.
    /// </summary>
    public float? Pitch { get; init; }
}
