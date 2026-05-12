namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Preferred FFmpeg hardware acceleration mode for video decoding.
/// </summary>
public enum FfmpegHardwareAcceleration
{
    /// <summary>
    /// Let the stream worker choose the best available decoder.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Force CPU decoding.
    /// </summary>
    Cpu = 1,

    /// <summary>
    /// NVIDIA CUDA/NVDEC decoding.
    /// </summary>
    Cuda = 2,

    /// <summary>
    /// Windows DirectX Video Acceleration 2.
    /// </summary>
    Dxva2 = 3,

    /// <summary>
    /// Windows Direct3D 11 video acceleration.
    /// </summary>
    D3d11va = 4,

    /// <summary>
    /// Windows Direct3D 12 video acceleration.
    /// </summary>
    D3d12va = 5,

    /// <summary>
    /// Intel Quick Sync Video.
    /// </summary>
    Qsv = 6,

    /// <summary>
    /// Video Acceleration API.
    /// </summary>
    Vaapi = 7,

    /// <summary>
    /// OpenCL acceleration where supported by FFmpeg filters.
    /// </summary>
    OpenCl = 8,

    /// <summary>
    /// Vulkan acceleration where supported by FFmpeg filters.
    /// </summary>
    Vulkan = 9
}
