using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public sealed class FfmpegToolLocator : IFfmpegToolLocator
{
    private readonly FfmpegOptions _options;

    public FfmpegToolLocator(IOptions<FfmpegOptions> options)
    {
        _options = options.Value;
    }

    public string ResolveToolPath(string toolName)
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{toolName}.exe"
            : toolName;

        var candidates = new List<string>();

        var configuredPath = toolName.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase)
            ? _options.FFmpegPath
            : _options.FFprobePath;

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            candidates.Add(NormalizePath(configuredPath));
        }

        if (!string.IsNullOrWhiteSpace(_options.Directory))
        {
            candidates.Add(Path.Combine(NormalizePath(_options.Directory), executableName));
        }

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg", executableName));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "ffmpeg", executableName));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), executableName));

        var foundPath = candidates.FirstOrDefault(File.Exists);
        return foundPath ?? executableName;
    }

    private static string NormalizePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
    }
}
