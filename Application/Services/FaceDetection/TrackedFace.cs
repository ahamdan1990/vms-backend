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
}
