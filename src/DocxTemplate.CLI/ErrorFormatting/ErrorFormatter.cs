using DocxTemplate.Core.Models.Results;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DocxTemplate.CLI.ErrorFormatting;

/// <summary>
/// Formats errors for CLI output in various formats
/// </summary>
public static class ErrorFormatter
{
    /// <summary>
    /// Formats an error result for console output
    /// </summary>
    /// <param name="error">Error to format</param>
    /// <param name="includeDetails">Whether to include technical details</param>
    /// <returns>Formatted error message</returns>
    public static string FormatForConsole(ErrorResult error, bool includeDetails = false)
    {
        var sb = new StringBuilder();
        
        // Main error message
        sb.AppendLine(CultureInfo.InvariantCulture, $"Error: {error.Message}");
        
        // Add context if different from message
        if (!string.IsNullOrEmpty(error.OperationContext))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Operation: {error.OperationContext}");
        }
        
        // Add file path if available
        if (!string.IsNullOrEmpty(error.FilePath))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"File: {error.FilePath}");
        }
        
        // Add technical details if requested
        if (includeDetails && !string.IsNullOrEmpty(error.Details))
        {
            sb.AppendLine();
            sb.AppendLine("Technical Details:");
            sb.AppendLine(error.Details);
        }
        
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats an error result as JSON for pipeline output
    /// </summary>
    /// <param name="error">Error to format</param>
    /// <param name="indented">Whether to format with indentation</param>
    /// <returns>JSON representation of the error</returns>
    public static string FormatAsJson(ErrorResult error, bool indented = true)
    {
        var errorObject = new
        {
            success = false,
            timestamp = error.Timestamp.ToString("O"),
            error = new
            {
                type = error.ErrorType,
                message = error.Message,
                operation = error.OperationContext,
                details = error.Details,
                filePath = error.FilePath,
                exitCode = error.ExitCode,
                isCritical = error.IsCritical
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(errorObject, options);
    }

    /// <summary>
    /// Formats a command result for console output
    /// </summary>
    /// <typeparam name="T">Type of result data</typeparam>
    /// <param name="result">Command result to format</param>
    /// <param name="includeDetails">Whether to include technical details for errors</param>
    /// <returns>Formatted message</returns>
    public static string FormatCommandResult<T>(CommandResult<T> result, bool includeDetails = false)
    {
        if (result.IsSuccess)
        {
            return "Operation completed successfully.";
        }

        if (result.Error != null)
        {
            return FormatForConsole(result.Error, includeDetails);
        }

        return "Operation failed with unknown error.";
    }

    /// <summary>
    /// Formats a command result as JSON
    /// </summary>
    /// <typeparam name="T">Type of result data</typeparam>
    /// <param name="result">Command result to format</param>
    /// <param name="indented">Whether to format with indentation</param>
    /// <returns>JSON representation</returns>
    public static string FormatCommandResultAsJson<T>(CommandResult<T> result, bool indented = true)
    {
        object responseObject;

        if (result.IsSuccess)
        {
            responseObject = new
            {
                success = true,
                timestamp = result.CompletedAt.ToString("O"),
                data = result.Data
            };
        }
        else
        {
            responseObject = new
            {
                success = false,
                timestamp = result.CompletedAt.ToString("O"),
                error = result.Error != null ? new
                {
                    type = result.Error.ErrorType,
                    message = result.Error.Message,
                    operation = result.Error.OperationContext,
                    details = result.Error.Details,
                    filePath = result.Error.FilePath,
                    exitCode = result.Error.ExitCode,
                    isCritical = result.Error.IsCritical
                } : null
            };
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(responseObject, options);
    }

    /// <summary>
    /// Gets a short error summary suitable for status lines
    /// </summary>
    /// <param name="error">Error to summarize</param>
    /// <returns>Short error summary</returns>
    public static string GetShortSummary(ErrorResult error)
    {
        var prefix = error.IsCritical ? "CRITICAL" : "ERROR";
        var fileName = !string.IsNullOrEmpty(error.FilePath) 
            ? Path.GetFileName(error.FilePath) 
            : null;

        if (!string.IsNullOrEmpty(fileName))
        {
            return $"{prefix}: {fileName} - {error.Message}";
        }

        return $"{prefix}: {error.Message}";
    }
}