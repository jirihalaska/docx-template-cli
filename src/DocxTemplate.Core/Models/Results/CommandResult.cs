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
}

/// <summary>
/// Factory methods for creating CommandResult instances
/// </summary>
public static class CommandResult
{
    /// <summary>
    /// Creates a successful command result
    /// </summary>
    public static CommandResult<T> Success<T>(T data)
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
    public static CommandResult<T> Failure<T>(ErrorResult error)
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
    public static CommandResult<T> Failure<T>(Exception exception, string operationContext)
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

    /// <summary>
    /// Creates a successful command result without data
    /// </summary>
    public static CommandResultVoid Success()
    {
        return new CommandResultVoid
        {
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed command result without data
    /// </summary>
    public static CommandResultVoid FailureVoid(ErrorResult error)
    {
        return new CommandResultVoid
        {
            IsSuccess = false,
            Error = error
        };
    }

    /// <summary>
    /// Creates a failed command result from an exception without data
    /// </summary>
    public static CommandResultVoid FailureVoid(Exception exception, string operationContext)
    {
        var errorResult = exception switch
        {
            FileNotFoundException => ErrorResult.FileNotFoundError(exception.Message, operationContext),
            UnauthorizedAccessException => ErrorResult.FileAccessError(exception.Message, operationContext, exception),
            ArgumentException => ErrorResult.ValidationError(exception.Message, operationContext, exception.ToString()),
            _ => ErrorResult.CriticalError(exception.Message, operationContext, exception)
        };

        return new CommandResultVoid
        {
            IsSuccess = false,
            Error = errorResult
        };
    }
}

/// <summary>
/// Command result without data payload
/// </summary>
public record CommandResultVoid : CommandResult<object>
{
}