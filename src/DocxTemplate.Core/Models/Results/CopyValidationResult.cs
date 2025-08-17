using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Result of validating a copy operation before execution
/// </summary>
public record CopyValidationResult
{
    /// <summary>
    /// Indicates whether the copy operation can proceed
    /// </summary>
    public required bool CanCopy { get; init; }

    /// <summary>
    /// List of validation errors that prevent copying
    /// </summary>
    [Required(ErrorMessage = "Errors collection is required")]
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// List of validation warnings found
    /// </summary>
    [Required(ErrorMessage = "Warnings collection is required")]
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Number of files that would be copied
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files to copy must be non-negative")]
    public required int FilesToCopy { get; init; }

    /// <summary>
    /// Number of files that would be overwritten
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files to overwrite must be non-negative")]
    public required int FilesToOverwrite { get; init; }

    /// <summary>
    /// Number of directories that would be created
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Directories to create must be non-negative")]
    public required int DirectoriesToCreate { get; init; }

    /// <summary>
    /// Total size of files to copy in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total size must be non-negative")]
    public required long TotalSizeBytes { get; init; }

    /// <summary>
    /// Available disk space at target location in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Available space must be non-negative")]
    public long AvailableSpaceBytes { get; init; }

    /// <summary>
    /// Files that would conflict with existing files
    /// </summary>
    [Required(ErrorMessage = "Conflicting files collection is required")]
    public required IReadOnlyList<FileConflict> ConflictingFiles { get; init; }

    /// <summary>
    /// Source path being validated
    /// </summary>
    [Required(ErrorMessage = "Source path is required")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Target path being validated
    /// </summary>
    [Required(ErrorMessage = "Target path is required")]
    public required string TargetPath { get; init; }

    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the validation process
    /// </summary>
    public TimeSpan ValidationDuration { get; init; }

    /// <summary>
    /// Indicates whether there are any issues (errors or warnings)
    /// </summary>
    public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;

    /// <summary>
    /// Indicates whether there's sufficient disk space
    /// </summary>
    public bool HasSufficientSpace => AvailableSpaceBytes >= TotalSizeBytes;

    /// <summary>
    /// Percentage of available disk space that would be used
    /// </summary>
    public double SpaceUsagePercentage => 
        AvailableSpaceBytes > 0 ? (TotalSizeBytes * 100.0) / AvailableSpaceBytes : 0;

    /// <summary>
    /// Gets a display-friendly total size string
    /// </summary>
    public string DisplayTotalSize => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Gets a display-friendly available space string
    /// </summary>
    public string DisplayAvailableSpace => FormatBytes(AvailableSpaceBytes);

    /// <summary>
    /// Gets a summary of the validation result
    /// </summary>
    public string GetSummary()
    {
        var summary = $"Copy validation: {(CanCopy ? "Can proceed" : "Cannot proceed")}. ";
        summary += $"{FilesToCopy} files ({DisplayTotalSize}) to copy";
        
        if (FilesToOverwrite > 0)
            summary += $", {FilesToOverwrite} would be overwritten";
            
        if (DirectoriesToCreate > 0)
            summary += $", {DirectoriesToCreate} directories to create";

        summary += $". Space: {DisplayTotalSize}/{DisplayAvailableSpace} ({SpaceUsagePercentage:F1}% usage). ";
        
        if (Errors.Count > 0)
            summary += $"{Errors.Count} error{(Errors.Count == 1 ? "" : "s")}. ";
            
        if (Warnings.Count > 0)
            summary += $"{Warnings.Count} warning{(Warnings.Count == 1 ? "" : "s")}. ";

        summary += $"Validated in {ValidationDuration.TotalMilliseconds:F0}ms.";

        return summary;
    }

    /// <summary>
    /// Gets all issues (errors and warnings) as a formatted string
    /// </summary>
    public string GetAllIssues()
    {
        var issues = new List<string>();
        
        foreach (var error in Errors)
            issues.Add($"ERROR: {error}");
            
        foreach (var warning in Warnings)
            issues.Add($"WARNING: {warning}");

        return string.Join("\n", issues);
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <param name="sourcePath">Source path</param>
    /// <param name="targetPath">Target path</param>
    /// <param name="filesToCopy">Number of files to copy</param>
    /// <param name="filesToOverwrite">Number of files to overwrite</param>
    /// <param name="directoriesToCreate">Number of directories to create</param>
    /// <param name="totalSizeBytes">Total size in bytes</param>
    /// <param name="availableSpaceBytes">Available disk space</param>
    /// <param name="validationDuration">Duration of validation</param>
    /// <param name="warnings">Optional warnings</param>
    /// <returns>CopyValidationResult instance</returns>
    public static CopyValidationResult Success(
        string sourcePath,
        string targetPath,
        int filesToCopy,
        int filesToOverwrite,
        int directoriesToCreate,
        long totalSizeBytes,
        long availableSpaceBytes,
        TimeSpan validationDuration,
        IReadOnlyList<string>? warnings = null)
    {
        return new CopyValidationResult
        {
            CanCopy = true,
            Errors = Array.Empty<string>(),
            Warnings = warnings ?? Array.Empty<string>(),
            FilesToCopy = filesToCopy,
            FilesToOverwrite = filesToOverwrite,
            DirectoriesToCreate = directoriesToCreate,
            TotalSizeBytes = totalSizeBytes,
            AvailableSpaceBytes = availableSpaceBytes,
            ConflictingFiles = Array.Empty<FileConflict>(),
            SourcePath = sourcePath,
            TargetPath = targetPath,
            ValidationDuration = validationDuration
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="sourcePath">Source path</param>
    /// <param name="targetPath">Target path</param>
    /// <param name="errors">Validation errors</param>
    /// <param name="filesToCopy">Number of files to copy</param>
    /// <param name="conflictingFiles">Files that would conflict</param>
    /// <param name="validationDuration">Duration of validation</param>
    /// <param name="warnings">Optional warnings</param>
    /// <returns>CopyValidationResult instance</returns>
    public static CopyValidationResult Failure(
        string sourcePath,
        string targetPath,
        IReadOnlyList<string> errors,
        int filesToCopy = 0,
        IReadOnlyList<FileConflict>? conflictingFiles = null,
        TimeSpan validationDuration = default,
        IReadOnlyList<string>? warnings = null)
    {
        return new CopyValidationResult
        {
            CanCopy = false,
            Errors = errors,
            Warnings = warnings ?? Array.Empty<string>(),
            FilesToCopy = filesToCopy,
            FilesToOverwrite = 0,
            DirectoriesToCreate = 0,
            TotalSizeBytes = 0,
            AvailableSpaceBytes = 0,
            ConflictingFiles = conflictingFiles ?? Array.Empty<FileConflict>(),
            SourcePath = sourcePath,
            TargetPath = targetPath,
            ValidationDuration = validationDuration
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
/// Represents a file conflict during copy validation
/// </summary>
public record FileConflict
{
    /// <summary>
    /// Source file path
    /// </summary>
    [Required(ErrorMessage = "Source file path is required")]
    public required string SourceFilePath { get; init; }

    /// <summary>
    /// Target file path that would conflict
    /// </summary>
    [Required(ErrorMessage = "Target file path is required")]
    public required string TargetFilePath { get; init; }

    /// <summary>
    /// Type of conflict
    /// </summary>
    public ConflictType ConflictType { get; init; }

    /// <summary>
    /// Size of the source file
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Source size must be non-negative")]
    public long SourceSizeBytes { get; init; }

    /// <summary>
    /// Size of the existing target file
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Target size must be non-negative")]
    public long TargetSizeBytes { get; init; }

    /// <summary>
    /// Last modified time of the source file
    /// </summary>
    public DateTime SourceLastModified { get; init; }

    /// <summary>
    /// Last modified time of the existing target file
    /// </summary>
    public DateTime TargetLastModified { get; init; }

    /// <summary>
    /// Source file name
    /// </summary>
    public string SourceFileName => Path.GetFileName(SourceFilePath);

    /// <summary>
    /// Target file name
    /// </summary>
    public string TargetFileName => Path.GetFileName(TargetFilePath);

    /// <summary>
    /// Indicates whether the source file is newer
    /// </summary>
    public bool IsSourceNewer => SourceLastModified > TargetLastModified;

    /// <summary>
    /// Indicates whether the source file is larger
    /// </summary>
    public bool IsSourceLarger => SourceSizeBytes > TargetSizeBytes;

    /// <summary>
    /// Gets a display string for this conflict
    /// </summary>
    public string DisplayConflict => $"{TargetFileName}: {ConflictType} conflict";
}

/// <summary>
/// Types of file conflicts
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// File already exists at target location
    /// </summary>
    FileExists,

    /// <summary>
    /// Directory exists where file would be placed
    /// </summary>
    DirectoryExists,

    /// <summary>
    /// File is read-only and cannot be overwritten
    /// </summary>
    ReadOnlyFile,

    /// <summary>
    /// File is in use and cannot be overwritten
    /// </summary>
    FileInUse,

    /// <summary>
    /// Insufficient permissions to overwrite
    /// </summary>
    InsufficientPermissions
}