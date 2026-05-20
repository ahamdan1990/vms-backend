using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using VisitorManagementSystem.Api.Application.Services.Cameras;

namespace VisitorManagementSystem.Api.Infrastructure.Services;

/// <summary>
/// Saves face crop images under wwwroot/face-snapshots/{category}/{cameraId}/{yyyy}/{MM}/{dd}/{guid}.jpg
/// and serves them as static files.
/// </summary>
public sealed class FileSystemFaceSnapshotService : IFaceSnapshotService
{
    private const string SnapshotRoot = "face-snapshots";
    private const int MaxFaceDimension = 360;
    private const int JpegQuality = 82;
    private const int PaddingFactor = 35;
    private const int MinPadding = 20;

    private readonly string _webRootPath;
    private readonly ILogger<FileSystemFaceSnapshotService> _logger;

    public FileSystemFaceSnapshotService(
        IWebHostEnvironment env,
        ILogger<FileSystemFaceSnapshotService> logger)
    {
        _webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> SaveFaceCropAsync(
        byte[] frameBytes,
        int x, int y, int width, int height,
        int cameraId,
        string category,
        int? personId = null,
        string? personType = null,
        CancellationToken cancellationToken = default)
    {
        if (frameBytes is not { Length: > 0 })
            return null;

        if (width < 20 || height < 20)
            return null;

        try
        {
            using var image = Image.Load(frameBytes);

            var paddingX = Math.Max(MinPadding, (int)(width * PaddingFactor / 100.0));
            var paddingY = Math.Max(MinPadding, (int)(height * PaddingFactor / 100.0));
            var cropX = Math.Clamp(x - paddingX, 0, Math.Max(0, image.Width - 1));
            var cropY = Math.Clamp(y - paddingY, 0, Math.Max(0, image.Height - 1));
            var cropRight = Math.Clamp(x + width + paddingX, cropX + 1, image.Width);
            var cropBottom = Math.Clamp(y + height + paddingY, cropY + 1, image.Height);
            var cropRect = new Rectangle(cropX, cropY, cropRight - cropX, cropBottom - cropY);

            image.Mutate(ctx =>
            {
                ctx.Crop(cropRect);
                if (cropRect.Width > MaxFaceDimension || cropRect.Height > MaxFaceDimension)
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(MaxFaceDimension, MaxFaceDimension),
                        Mode = ResizeMode.Max
                    });
                }
            });

            var now = DateTime.UtcNow;
            var safeCategory = SanitizeSegment(category);
            string relativePath;
            if (personId.HasValue && !string.IsNullOrWhiteSpace(personType))
            {
                relativePath = Path.Combine(
                    SnapshotRoot, safeCategory,
                    SanitizeSegment(personType), personId.Value.ToString(),
                    now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"),
                    $"{Guid.NewGuid():N}.jpg");
            }
            else
            {
                relativePath = Path.Combine(
                    SnapshotRoot, safeCategory, cameraId.ToString(),
                    now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"),
                    $"{Guid.NewGuid():N}.jpg");
            }

            var absolutePath = Path.Combine(_webRootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            await using var fs = File.Create(absolutePath);
            await image.SaveAsJpegAsync(fs, new JpegEncoder { Quality = JpegQuality }, cancellationToken);

            return relativePath.Replace('\\', '/');
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save face snapshot for camera {CameraId}", cameraId);
            return null;
        }
    }

    public void DeleteSnapshot(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        try
        {
            var absolutePath = Path.Combine(_webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete face snapshot {Path}", relativePath);
        }
    }

    public string GetSnapshotUrl(string relativePath)
    {
        return "/" + relativePath.Replace('\\', '/');
    }

    private static string SanitizeSegment(string segment)
    {
        return string.IsNullOrWhiteSpace(segment) ? "misc" :
            new string(segment.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_').ToArray());
    }
}
