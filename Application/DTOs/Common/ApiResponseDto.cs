using System.Text.Json.Serialization;

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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; set; }

    /// <summary>
    /// Error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Response metadata
    /// </summary>
    public object? Metadata { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    ///  <param name="correlationId">Correlation Id</param>
    /// <returns>Successful API response</returns>
    public static ApiResponseDto<T> SuccessResponse(T? data, string? message = null, string? correlationId = null)
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };
    }


    /// <summary>
    /// Creates an error response
    /// </summary>
    /// <param name="errors">Error messages</param>
    /// <param name="message">Error message</param>
    /// <param name="correlationId">Correlation Id</param>
    /// <returns>Error API response</returns>
    public static ApiResponseDto<T> ErrorResponse(List<string> errors, string? message = null, string? correlationId = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Creates an error response with single error
    /// </summary>
    /// <param name="error">Error message</param>
    /// <param name="message">Error message</param>
    /// <param name="correlationId">Correlation Id</param>
    /// <returns>Error API response</returns>
    public static ApiResponseDto<T> ErrorResponse(string error, string? message = null, string? correlationId = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error },
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };
    }
}