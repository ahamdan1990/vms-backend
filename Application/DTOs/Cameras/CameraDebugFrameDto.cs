namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

public class CameraDebugFrameDto
{
    public bool HasFrame { get; set; }
    public string? FrameJpegBase64 { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int FrameSizeBytes { get; set; }
    public DateTime? FrameCapturedAt { get; set; }
    public string EngineUsed { get; set; } = "none";
    public bool EngineAvailable { get; set; }
    public int RawDetectionCount { get; set; }
    public int FilteredDetectionCount { get; set; }
    public List<CameraDebugFaceDto> Faces { get; set; } = [];
    public Dictionary<string, object> EngineStatus { get; set; } = new();
    public string? ErrorMessage { get; set; }
    // Active filter thresholds (for display)
    public double DetectionThreshold { get; set; }
    public int MinFaceSizePixels { get; set; }
    public int? MaxFaceSizePixels { get; set; }
    public double QualityThreshold { get; set; }
    public double BlurThreshold { get; set; }
    public int MaxFacesPerFrame { get; set; }
}

public class CameraDebugFaceDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Confidence { get; set; }
    public double? QualityScore { get; set; }
    public bool PassedFilter { get; set; }
    public List<string> FilterReasons { get; set; } = [];
}
