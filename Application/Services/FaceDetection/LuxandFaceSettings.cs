namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Configuration for the local Luxand FaceSDK engine.
/// </summary>
public class LuxandFaceSettings
{
    public const string SectionName = "LuxandFaceSDK";

    public bool Enabled { get; set; } = false;

    public string LicenseKey { get; set; } = string.Empty;

    public float MatchThreshold { get; set; } = 0.80f;

    public int DetectionThreshold { get; set; } = 5;

    public int InternalResizeWidth { get; set; } = 384;

    public int CropMarginPercent { get; set; } = 20;

    public int MaxAdditionalTemplatesPerIdentity { get; set; } = 5;

    /// <summary>
    /// When true, Luxand scans multiple rotations to detect faces at any angle.
    /// Most accurate but 2–4× slower. Set false for fixed-mount cameras where subjects are always upright.
    /// </summary>
    public bool ArbitraryRotationsEnabled { get; set; } = true;

    /// <summary>
    /// When true, Luxand computes the in-plane roll angle for each detected face.
    /// Required for RollLimitDegrees filtering to have any effect.
    /// Slight performance cost (~5%). Default true.
    /// </summary>
    public bool DetermineRotationAngle { get; set; } = true;

    /// <summary>
    /// When true, every frame passed into Luxand is written to DebugFrameDumpPath as a JPEG.
    /// Disabled by default — enable only for short diagnostic sessions.
    /// </summary>
    public bool DebugFrameDumpEnabled { get; set; } = false;

    /// <summary>
    /// Directory where debug frame JPEGs are written when DebugFrameDumpEnabled is true.
    /// </summary>
    public string DebugFrameDumpPath { get; set; } = "debug_frames";
}
