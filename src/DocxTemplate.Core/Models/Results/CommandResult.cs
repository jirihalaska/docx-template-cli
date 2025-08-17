namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Base result for command operations with success/failure status and error details
/// </summary>
/// <typeparam name="T">Type of data returned on success</typeparam>
public record CommandResult<T>
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Data returned on successful operations
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error information for failed operations
    /// </summary>
    public ErrorResult? Error { get; init; }

    /// <summary>
    /// Timestamp when the operation completed
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful command result
    /// </summary>
    public static CommandResult<T> Success(T data)
    {
        return new CommandResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed command result
    /// </summary>
    public static CommandResult<T> Failure(ErrorResult error)
    {
        return new CommandResult<T>
        {
            IsSuccess = false,
            Error = error
        };
    }

    /// <summary>
    /// Creates a failed command result from an exception
    /// </summary>
    public static CommandResult<T> Failure(Exception exception, string operationContext)
    {
        var errorResult = exception switch
        {
            FileNotFoundException => ErrorResult.FileNotFoundError(exception.Message, operationContext),
            UnauthorizedAccessException => ErrorResult.FileAccessError(exception.Message, operationContext, exception),
            ArgumentException => ErrorResult.ValidationError(exception.Message, operationContext, exception.ToString()),
            _ => ErrorResult.CriticalError(exception.Message, operationContext, exception)
        };

        return new CommandResult<T>
        {
            IsSuccess = false,
            Error = errorResult
        };
    }
}

/// <summary>
/// Command result without data payload
/// </summary>
public record CommandResult : CommandResult<object>
{
    /// <summary>
    /// Creates a successful command result without data
    /// </summary>
    public static CommandResult Success()
    {
        return new CommandResult
        {
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed command result without data
    /// </summary>
    public static CommandResult Failure(ErrorResult error)
    {
        return new CommandResult
        {
            IsSuccess = false,
            Error = error
        };
    }

    /// <summary>
    /// Creates a failed command result from an exception
    /// </summary>
    public static CommandResult Failure(Exception exception, string operationContext)
    {
        var errorResult = exception switch
        {
            FileNotFoundException => ErrorResult.FileNotFoundError(exception.Message, operationContext),
            UnauthorizedAccessException => ErrorResult.FileAccessError(exception.Message, operationContext, exception),
            ArgumentException => ErrorResult.ValidationError(exception.Message, operationContext, exception.ToString()),
            _ => ErrorResult.CriticalError(exception.Message, operationContext, exception)
        };

        return new CommandResult
        {
            IsSuccess = false,
            Error = errorResult
        };
    }
}