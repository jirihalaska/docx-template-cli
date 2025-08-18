using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Represents a collection of templates organized as a set
/// </summary>
public record TemplateSet
{
    /// <summary>
    /// Name of the template set (typically the directory name)
    /// </summary>
    [Required(ErrorMessage = "Template set name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Template set name must be between 1 and 255 characters")]
    public required string Name { get; init; }

    /// <summary>
    /// Full path to the template set directory
    /// </summary>
    [Required(ErrorMessage = "Template set full path is required")]
    public required string FullPath { get; init; }

    /// <summary>
    /// All templates contained in this set
    /// </summary>
    [Required(ErrorMessage = "Templates collection is required")]
    public required IReadOnlyList<Template> Templates { get; init; }

    /// <summary>
    /// Number of templates in this set
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Template count must be non-negative")]
    public required int TemplateCount { get; init; }

    /// <summary>
    /// Total size of all templates in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total size must be non-negative")]
    public required long TotalSizeBytes { get; init; }

    /// <summary>
    /// Last modification time of the template set (latest template modification)
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Indicates whether the template set contains subdirectories
    /// </summary>
    public required bool HasSubfolders { get; init; }

    /// <summary>
    /// Optional description of the template set
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional version information
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Optional tags for categorizing the template set
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets a display-friendly size string (e.g., "2.3 MB")
    /// </summary>
    public string DisplaySize => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Gets the directory depth of the template set
    /// </summary>
    public int DirectoryDepth
    {
        get
        {
            if (!HasSubfolders) return 1;
            
            return Templates.Select(t => t.RelativePath.Split(Path.DirectorySeparatorChar).Length)
                           .DefaultIfEmpty(1)
                           .Max();
        }
    }

    /// <summary>
    /// Gets templates grouped by their directory
    /// </summary>
    public IEnumerable<IGrouping<string, Template>> TemplatesByDirectory =>
        Templates.GroupBy(t => Path.GetDirectoryName(t.RelativePath) ?? string.Empty);

    /// <summary>
    /// Gets the average template size
    /// </summary>
    public double AverageTemplateSize => 
        TemplateCount > 0 ? (double)TotalSizeBytes / TemplateCount : 0;

    /// <summary>
    /// Validates the template set data integrity
    /// </summary>
    /// <returns>True if the template set data is consistent</returns>
    public bool IsValid()
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(FullPath))
            return false;

        // Check that template count matches templates collection count
        if (TemplateCount != Templates.Count)
            return false;

        // Check that total size matches sum of template sizes
        var calculatedSize = Templates.Sum(t => t.SizeBytes);
        if (TotalSizeBytes != calculatedSize)
            return false;

        // Check that all templates are valid
        if (Templates.Any(t => !t.IsValid()))
            return false;

        // Check that all templates belong to this set
        if (Templates.Any(t => !t.FullPath.StartsWith(FullPath, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Check that last modified is reasonable (not in the future)
        if (LastModified > DateTime.UtcNow.AddMinutes(5)) // Allow small clock differences
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the template set directory exists
    /// </summary>
    /// <returns>True if the directory exists</returns>
    public bool DirectoryExists() => Directory.Exists(FullPath);

    /// <summary>
    /// Gets templates with a specific file extension
    /// </summary>
    /// <param name="extension">File extension (with or without dot)</param>
    /// <returns>Templates with the specified extension</returns>
    public IEnumerable<Template> GetTemplatesByExtension(string extension)
    {
        var normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";
        return Templates.Where(t => t.FullPath.EndsWith(normalizedExtension, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets templates in a specific subdirectory
    /// </summary>
    /// <param name="subdirectory">Subdirectory path relative to template set root</param>
    /// <returns>Templates in the specified subdirectory</returns>
    public IEnumerable<Template> GetTemplatesInSubdirectory(string subdirectory)
    {
        var normalizedSubdir = subdirectory.Replace('\\', Path.DirectorySeparatorChar)
                                         .Replace('/', Path.DirectorySeparatorChar);
        
        return Templates.Where(t => 
        {
            var templateDir = Path.GetDirectoryName(t.RelativePath) ?? string.Empty;
            return templateDir.Equals(normalizedSubdir, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Gets a summary string of the template set
    /// </summary>
    public string GetSummary()
    {
        return $"{Name}: {TemplateCount} template{(TemplateCount == 1 ? "" : "s")} ({DisplaySize})" +
               (HasSubfolders ? $" across {DirectoryDepth} directory level{(DirectoryDepth == 1 ? "" : "s")}" : "");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.#} {sizes[order]}";
    }
}

/// <summary>
/// Result of validating a template set
/// </summary>
public record TemplateSetValidationResult
{
    /// <summary>
    /// Indicates whether the template set is valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors found
    /// </summary>
    [Required(ErrorMessage = "Errors collection is required")]
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// List of validation warnings found
    /// </summary>
    [Required(ErrorMessage = "Warnings collection is required")]
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Timestamp when the validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional details about the validation process
    /// </summary>
    public string? ValidationDetails { get; init; }

    /// <summary>
    /// Number of files that were validated
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files validated must be non-negative")]
    public int FilesValidated { get; init; }

    /// <summary>
    /// Duration of the validation process
    /// </summary>
    public TimeSpan ValidationDuration { get; init; }

    /// <summary>
    /// Indicates whether there are any issues (errors or warnings)
    /// </summary>
    public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;

    /// <summary>
    /// Gets a summary of the validation result
    /// </summary>
    public string GetSummary()
    {
        if (IsValid && !HasIssues)
            return $"Template set is valid. Validated {FilesValidated} files in {ValidationDuration.TotalMilliseconds:F0}ms.";

        var summary = $"Template set validation: {(IsValid ? "Valid" : "Invalid")}. ";
        
        if (Errors.Count > 0)
            summary += $"{Errors.Count} error{(Errors.Count == 1 ? "" : "s")}. ";
            
        if (Warnings.Count > 0)
            summary += $"{Warnings.Count} warning{(Warnings.Count == 1 ? "" : "s")}. ";

        summary += $"Validated {FilesValidated} files in {ValidationDuration.TotalMilliseconds:F0}ms.";

        return summary;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <param name="filesValidated">Number of files validated</param>
    /// <param name="duration">Validation duration</param>
    /// <param name="warnings">Optional warnings</param>
    /// <returns>TemplateSetValidationResult instance</returns>
    public static TemplateSetValidationResult Success(
        int filesValidated = 0, 
        TimeSpan duration = default,
        IReadOnlyList<string>? warnings = null)
    {
        return new TemplateSetValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = warnings ?? [],
            FilesValidated = filesValidated,
            ValidationDuration = duration
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="filesValidated">Number of files validated</param>
    /// <param name="duration">Validation duration</param>
    /// <param name="warnings">Optional warnings</param>
    /// <returns>TemplateSetValidationResult instance</returns>
    public static TemplateSetValidationResult Failure(
        IReadOnlyList<string> errors, 
        int filesValidated = 0,
        TimeSpan duration = default,
        IReadOnlyList<string>? warnings = null)
    {
        return new TemplateSetValidationResult
        {
            IsValid = false,
            Errors = errors,
            Warnings = warnings ?? [],
            FilesValidated = filesValidated,
            ValidationDuration = duration
        };
    }
}