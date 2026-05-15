namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Saves face-crop images to the local file system and maps relative paths to URLs.
/// </summary>
public interface IFaceSnapshotService
{
    /// <summary>
    /// Crops the bounding-box region from <paramref name="frameBytes"/>, resizes, and saves to disk.
    /// Returns the relative path (relative to wwwroot) or null if saving fails.
    /// </summary>
    Task<string?> SaveFaceCropAsync(
        byte[] frameBytes,
        int x, int y, int width, int height,
        int cameraId,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the snapshot file identified by its relative path. Silently ignores missing files.
    /// </summary>
    void DeleteSnapshot(string relativePath);

    /// <summary>
    /// Converts a stored relative path to an absolute URL the client can request.
    /// </summary>
    string GetSnapshotUrl(string relativePath);
}
