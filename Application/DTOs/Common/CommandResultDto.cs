public class CommandResultDto<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public T? Data { get; set; }

    public static CommandResultDto<T> Success(T? data = default, string? message = null)
    {
        return new CommandResultDto<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static CommandResultDto<T> Failure(string message, List<string>? errors = null)
    {
        return new CommandResultDto<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors ?? new List<string> { message }
        };
    }

    public static CommandResultDto<T> Failure(List<string> errors)
    {
        return new CommandResultDto<T>
        {
            IsSuccess = false,
            Message = errors.FirstOrDefault(),
            Errors = errors
        };
    }
}
