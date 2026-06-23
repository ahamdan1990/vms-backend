namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Configuration for the Primary face engine (local SDK).
/// </summary>
public class LuxandFaceSettings
{
    public const string SectionName = "FaceEngine";

    public bool Enabled { get; set; } = false;

    public string LicenseKey { get; set; } = string.Empty;

    public float MatchThreshold { get; set; } = 0.80f;

    /// <summary>
    /// Luxand tracker's internal face-to-track similarity threshold (0–1).
    /// Controls when two face appearances are merged into the same tracker ID.
    /// Separate from MatchThreshold, which governs our own FSDK.MatchFaces calls.
    /// Default 0.992 matches Luxand's recommendation (FAR ~0.000081 per SDK FAR/FRR table).
    /// </summary>
    public double TrackerThreshold { get; set; } = 0.992;

    public int DetectionThreshold { get; set; } = 5;

    public int InternalResizeWidth { get; set; } = 640;

    public int CropMarginPercent { get; set; } = 20;

    public int MaxAdditionalTemplatesPerIdentity { get; set; } = 5;

    public bool ArbitraryRotationsEnabled { get; set; } = true;

    public bool DetermineRotationAngle { get; set; } = true;

    public bool DebugFrameDumpEnabled { get; set; } = false;

    public string DebugFrameDumpPath { get; set; } = "debug_frames";
}
