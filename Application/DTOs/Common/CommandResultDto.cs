namespace VisitorManagementSystem.Api.Application.DTOs.Common;

/// <summary>
/// Command result data transfer object
/// </summary>
public class CommandResultDto
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Additional data
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <param name="message">Success message</param>
    /// <param name="data">Additional data</param>
    /// <returns>Successful command result</returns>
    public static CommandResultDto Success(string? message = null, object? data = null)
    {
        return new CommandResultDto
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failure result
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Error list</param>
    /// <returns>Failed command result</returns>
    public static CommandResultDto Failure(string message, List<string>? errors = null)
    {
        return new CommandResultDto
        {
            IsSuccess = false,
            Message = message,
            Errors = errors ?? new List<string> { message }
        };
    }
}
