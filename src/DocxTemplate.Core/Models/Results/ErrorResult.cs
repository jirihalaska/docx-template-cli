namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Result containing error information for operations that fail
/// </summary>
public record ErrorResult
{
    /// <summary>
    /// Type of error that occurred
    /// </summary>
    public required string ErrorType { get; init; }

    /// <summary>
    /// User-friendly error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Context about what operation was being performed
    /// </summary>
    public required string OperationContext { get; init; }

    /// <summary>
    /// Detailed technical error information (for debugging)
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// File path associated with the error (if applicable)
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Exit code to use for CLI operations
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Whether this is a critical error that should stop pipeline execution
    /// </summary>
    public bool IsCritical { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates an error result for a file access error
    /// </summary>
    public static ErrorResult FileAccessError(string filePath, string operation, Exception exception)
    {
        return new ErrorResult
        {
            ErrorType = "FileAccess",
            Message = $"Cannot access file '{filePath}' for {operation}: {exception.Message}",
            OperationContext = operation,
            Details = exception.ToString(),
            FilePath = filePath,
            ExitCode = 3,
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates an error result for a file not found error
    /// </summary>
    public static ErrorResult FileNotFoundError(string filePath, string operation)
    {
        return new ErrorResult
        {
            ErrorType = "FileNotFound",
            Message = $"File '{filePath}' not found for {operation}",
            OperationContext = operation,
            FilePath = filePath,
            ExitCode = 2,
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates an error result for a validation error
    /// </summary>
    public static ErrorResult ValidationError(string message, string operation, string? details = null)
    {
        return new ErrorResult
        {
            ErrorType = "Validation",
            Message = message,
            OperationContext = operation,
            Details = details,
            ExitCode = 1,
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates an error result for a document processing error
    /// </summary>
    public static ErrorResult DocumentProcessingError(string filePath, string operation, Exception exception)
    {
        return new ErrorResult
        {
            ErrorType = "DocumentProcessing",
            Message = $"Failed to process document '{filePath}' during {operation}: {exception.Message}",
            OperationContext = operation,
            Details = exception.ToString(),
            FilePath = filePath,
            ExitCode = 4,
            IsCritical = false
        };
    }

    /// <summary>
    /// Creates an error result for a critical system error
    /// </summary>
    public static ErrorResult CriticalError(string message, string operation, Exception exception)
    {
        return new ErrorResult
        {
            ErrorType = "Critical",
            Message = message,
            OperationContext = operation,
            Details = exception.ToString(),
            ExitCode = 99,
            IsCritical = true
        };
    }
}