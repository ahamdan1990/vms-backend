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

    public int DetectionThreshold { get; set; } = 5;

    public int InternalResizeWidth { get; set; } = 640;

    public int CropMarginPercent { get; set; } = 20;

    public int MaxAdditionalTemplatesPerIdentity { get; set; } = 5;

    public bool ArbitraryRotationsEnabled { get; set; } = true;

    public bool DetermineRotationAngle { get; set; } = true;

    public bool DebugFrameDumpEnabled { get; set; } = false;

    public string DebugFrameDumpPath { get; set; } = "debug_frames";
}
