using DocxTemplate.Core.Models.Results;

namespace DocxTemplate.Core.ErrorHandling;

/// <summary>
/// Service for handling exceptions and formatting error messages
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Handles an exception and returns formatted error information
    /// </summary>
    /// <param name="exception">The exception to handle</param>
    /// <param name="operationContext">Context about what operation was being performed</param>
    /// <returns>Error result with formatted message and details</returns>
    Task<ErrorResult> HandleExceptionAsync(Exception exception, string operationContext);

    /// <summary>
    /// Formats an error message for user display
    /// </summary>
    /// <param name="exception">The exception to format</param>
    /// <param name="context">Context about what operation was being performed</param>
    /// <returns>User-friendly error message</returns>
    string FormatErrorMessage(Exception exception, string context);

    /// <summary>
    /// Gets the appropriate exit code for an exception
    /// </summary>
    /// <param name="exception">The exception to map to an exit code</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    int GetExitCode(Exception exception);

    /// <summary>
    /// Determines if an exception is critical (should stop pipeline execution)
    /// </summary>
    /// <param name="exception">The exception to evaluate</param>
    /// <returns>True if the exception is critical</returns>
    bool IsCriticalError(Exception exception);
}