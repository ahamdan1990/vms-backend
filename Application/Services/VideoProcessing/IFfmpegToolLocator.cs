namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public interface IFfmpegToolLocator
{
    string ResolveToolPath(string toolName);
}
