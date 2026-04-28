using MediatR;
using Microsoft.AspNetCore.Http;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Runs face recognition on a captured photo, checks VIP/blacklist classification,
/// and fires appropriate real-time alerts — all in one atomic operation.
/// </summary>
public class RecognizeAndClassifyVisitorCommand : IRequest<RecognitionResultDto>
{
    public IFormFile Photo { get; set; } = null!;

    /// <summary>Location name used in alert messages (e.g. "Main Reception")</summary>
    public string Location { get; set; } = "Reception";
}
