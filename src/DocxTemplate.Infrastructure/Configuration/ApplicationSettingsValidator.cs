using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace DocxTemplate.Infrastructure.Configuration;

public class ApplicationSettingsValidator : IValidateOptions<ApplicationSettings>
{
    public ValidateOptionsResult Validate(string? name, ApplicationSettings options)
    {
        var failures = new List<string>();

        // Validate FileSystem settings
        if (options.FileSystem.MaxFileSizeMB <= 0)
        {
            failures.Add("FileSystem.MaxFileSizeMB must be greater than 0");
        }

        if (options.FileSystem.BackupRetentionDays < 0)
        {
            failures.Add("FileSystem.BackupRetentionDays must be non-negative");
        }

        try
        {
            System.Text.Encoding.GetEncoding(options.FileSystem.DefaultEncoding);
        }
        catch (ArgumentException)
        {
            failures.Add($"FileSystem.DefaultEncoding '{options.FileSystem.DefaultEncoding}' is not a valid encoding");
        }

        // Validate Template settings
        try
        {
            _ = new Regex(options.Templates.PlaceholderPattern);
        }
        catch (ArgumentException ex)
        {
            failures.Add($"Templates.PlaceholderPattern is not a valid regex: {ex.Message}");
        }

        if (options.Templates.SupportedExtensions == null || options.Templates.SupportedExtensions.Length == 0)
        {
            failures.Add("Templates.SupportedExtensions must contain at least one extension");
        }

        if (options.Templates.MaxConcurrentOperations <= 0)
        {
            failures.Add("Templates.MaxConcurrentOperations must be greater than 0");
        }

        // Validate Performance settings
        if (options.Performance.IoTimeoutSeconds <= 0)
        {
            failures.Add("Performance.IoTimeoutSeconds must be greater than 0");
        }

        if (options.Performance.MemoryLimitMB <= 0)
        {
            failures.Add("Performance.MemoryLimitMB must be greater than 0");
        }

        if (options.Performance.BatchSize <= 0)
        {
            failures.Add("Performance.BatchSize must be greater than 0");
        }

        // Validate Logging settings
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None" };
        if (!validLogLevels.Contains(options.Logging.LogLevel, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"Logging.LogLevel must be one of: {string.Join(", ", validLogLevels)}");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}