using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models.Results;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for handling exceptions and formatting error messages
/// </summary>
public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ErrorResult> HandleExceptionAsync(Exception exception, string operationContext)
    {
        var errorResult = CreateErrorResult(exception, operationContext);
        
        // Log the error with appropriate level
        if (errorResult.IsCritical)
        {
            _logger.LogError(exception, "Critical error during {OperationContext}: {Message}", 
                operationContext, errorResult.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Error during {OperationContext}: {Message}", 
                operationContext, errorResult.Message);
        }

        return await Task.FromResult(errorResult);
    }

    /// <inheritdoc />
    public string FormatErrorMessage(Exception exception, string context)
    {
        return exception switch
        {
            // Domain exceptions with business context
            TemplateNotFoundException ex => $"Template not found: {ex.Message}",
            FileAccessException ex => $"File access error: {ex.Message}",
            DocumentProcessingException ex => $"Document processing failed: {ex.Message}",
            ReplacementValidationException ex => $"Replacement validation failed: {ex.Message}",
            InvalidPlaceholderPatternException ex => $"Invalid placeholder pattern: {ex.Message}",
            
            // System exceptions
            FileNotFoundException ex => $"File not found: {Path.GetFileName(ex.FileName ?? "Unknown file")}",
            DirectoryNotFoundException ex => $"Directory not found: {ex.Message}",
            UnauthorizedAccessException => "Access denied. Check file permissions and try again.",
            IOException ex => $"File operation failed: {ex.Message}",
            
            // Validation exceptions (specific first)
            ArgumentNullException ex => $"Required parameter missing: {ex.ParamName}",
            ArgumentException ex => $"Invalid input: {ex.Message}",
            
            // JSON/serialization errors
            System.Text.Json.JsonException ex => $"Invalid JSON format: {ex.Message}",
            
            // Generic handling
            _ => $"An error occurred during {context}: {exception.Message}"
        };
    }

    /// <inheritdoc />
    public int GetExitCode(Exception exception)
    {
        return exception switch
        {
            // Success
            null => 0,
            
            // Domain exceptions (most specific first)
            ReplacementValidationException => 1,
            InvalidPlaceholderPatternException => 1,
            TemplateNotFoundException => 2,
            FileAccessException => 3,
            DocumentProcessingException => 4,
            
            // System exceptions (specific first for inheritance)
            ArgumentNullException => 1,
            ArgumentException => 1,
            System.Text.Json.JsonException => 1,
            
            FileNotFoundException => 2,
            DirectoryNotFoundException => 2,
            UnauthorizedAccessException => 3,
            
            // Critical system errors (specific first)
            InvalidOperationException => 99,
            
            // I/O exceptions (after more specific ones)
            IOException => 4,
            
            // SystemException is very broad, put last
            SystemException => 99,
            
            // Default for unknown exceptions
            _ => 10
        };
    }

    /// <inheritdoc />
    public bool IsCriticalError(Exception exception)
    {
        return exception switch
        {
            // Domain exceptions (not critical)
            ReplacementValidationException => false,
            InvalidPlaceholderPatternException => false,
            TemplateNotFoundException => false,
            FileAccessException => false,
            DocumentProcessingException => false,
            
            // Critical system exceptions (specific first)
            InvalidOperationException => true,
            
            // Non-critical system exceptions (specific first for inheritance)
            ArgumentNullException => false,
            ArgumentException => false,
            FileNotFoundException => false,
            UnauthorizedAccessException => false,
            System.Text.Json.JsonException => false,
            IOException => false,
            
            // SystemException is broad, put after specific ones
            SystemException => true,
            
            // Default to non-critical for unknown exceptions
            _ => false
        };
    }

    private ErrorResult CreateErrorResult(Exception exception, string operationContext)
    {
        var message = FormatErrorMessage(exception, operationContext);
        var exitCode = GetExitCode(exception);
        var isCritical = IsCriticalError(exception);

        return exception switch
        {
            FileNotFoundException ex => ErrorResult.FileNotFoundError(
                ex.FileName ?? "Unknown file", operationContext),
            
            UnauthorizedAccessException => ErrorResult.FileAccessError(
                "File or directory", operationContext, exception),
            
            ArgumentException => ErrorResult.ValidationError(
                message, operationContext, exception.ToString()),
            
            DocumentProcessingException => ErrorResult.DocumentProcessingError(
                "Document", operationContext, exception),
            
            _ when isCritical => ErrorResult.CriticalError(
                message, operationContext, exception),
            
            _ => new ErrorResult
            {
                ErrorType = exception.GetType().Name,
                Message = message,
                OperationContext = operationContext,
                Details = exception.ToString(),
                ExitCode = exitCode,
                IsCritical = isCritical
            }
        };
    }
}