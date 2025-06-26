namespace VisitorManagementSystem.Api.Application.DTOs.Common;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Response data type</typeparam>
public class ApiResponseDto<T>
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Response metadata
    /// </summary>
    public object? Metadata { get; set; }

    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>Successful API response</returns>
    public static ApiResponseDto<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="errors">Error messages</param>
    /// <param name="message">Error message</param>
    /// <returns>Error API response</returns>
    public static ApiResponseDto<T> ErrorResponse(List<string> errors, string? message = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates an error response with single error
    /// </summary>
    /// <param name="error">Error message</param>
    /// <param name="message">Error message</param>
    /// <returns>Error API response</returns>
    public static ApiResponseDto<T> ErrorResponse(string error, string? message = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error }
        };
    }
}