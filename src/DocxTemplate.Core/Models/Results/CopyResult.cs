using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Result of a template copy operation
/// </summary>
public record CopyResult
{
    /// <summary>
    /// Number of files that were copied
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files count must be non-negative")]
    public required int FilesCount { get; init; }

    /// <summary>
    /// Total size in bytes of all copied files
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total bytes count must be non-negative")]
    public required long TotalBytesCount { get; init; }

    /// <summary>
    /// Details of all files that were copied
    /// </summary>
    [Required(ErrorMessage = "Copied files collection is required")]
    public required IReadOnlyList<CopiedFile> CopiedFiles { get; init; }

    /// <summary>
    /// Duration of the copy operation
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of files that failed to copy
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Failed files count must be non-negative")]
    public required int FailedFiles { get; init; }

    /// <summary>
    /// List of errors encountered during copying
    /// </summary>
    public IReadOnlyList<CopyError> Errors { get; init; } = Array.Empty<CopyError>();

    /// <summary>
    /// Number of files successfully copied
    /// </summary>
    public int SuccessfulFiles => FilesCount;

    /// <summary>
    /// Total number of files attempted (successful + failed)
    /// </summary>
    public int TotalFilesAttempted => FilesCount + FailedFiles;

    /// <summary>
    /// Copy rate in files per second
    /// </summary>
    public double FilesPerSecond => 
        Duration.TotalSeconds > 0 ? FilesCount / Duration.TotalSeconds : 0;

    /// <summary>
    /// Copy rate in bytes per second
    /// </summary>
    public double BytesPerSecond => 
        Duration.TotalSeconds > 0 ? TotalBytesCount / Duration.TotalSeconds : 0;

    /// <summary>
    /// Average file size in bytes
    /// </summary>
    public double AverageFileSize => 
        FilesCount > 0 ? (double)TotalBytesCount / FilesCount : 0;

    /// <summary>
    /// Indicates whether the operation was completely successful
    /// </summary>
    public bool IsCompletelySuccessful => FailedFiles == 0 && Errors.Count == 0;

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRatePercentage => 
        TotalFilesAttempted > 0 ? (FilesCount * 100.0) / TotalFilesAttempted : 100.0;

    /// <summary>
    /// Gets a display-friendly total size string
    /// </summary>
    public string DisplayTotalSize => FormatBytes(TotalBytesCount);

    /// <summary>
    /// Gets a display-friendly throughput string
    /// </summary>
    public string DisplayThroughput => $"{FormatBytes((long)BytesPerSecond)}/s";

    /// <summary>
    /// Validates the copy result data integrity
    /// </summary>
    /// <returns>True if the result data is consistent</returns>
    public bool IsValid()
    {
        // Check that files count matches copied files count
        if (FilesCount != CopiedFiles.Count)
            return false;

        // Check that total bytes matches sum of copied file sizes
        var calculatedBytes = CopiedFiles.Sum(f => f.SizeInBytes);
        if (TotalBytesCount != calculatedBytes)
            return false;

        // Check duration is non-negative
        if (Duration < TimeSpan.Zero)
            return false;

        // Check failed files count
        if (FailedFiles < 0)
            return false;

        // Check that all copied files are valid
        if (CopiedFiles.Any(f => !f.IsValid()))
            return false;

        return true;
    }

    /// <summary>
    /// Gets a summary string of the copy results
    /// </summary>
    public string GetSummary()
    {
        return $"Copied {FilesCount} files ({DisplayTotalSize}) in {Duration.TotalMilliseconds:F0}ms. " +
               (FailedFiles > 0 ? $"{FailedFiles} files failed. " : "") +
               $"Rate: {FilesPerSecond:F1} files/sec ({DisplayThroughput})";
    }

    /// <summary>
    /// Creates a successful copy result
    /// </summary>
    /// <param name="copiedFiles">List of successfully copied files</param>
    /// <param name="duration">Duration of the operation</param>
    /// <returns>CopyResult instance</returns>
    public static CopyResult Success(IReadOnlyList<CopiedFile> copiedFiles, TimeSpan duration)
    {
        var totalBytes = copiedFiles.Sum(f => f.SizeInBytes);

        return new CopyResult
        {
            FilesCount = copiedFiles.Count,
            TotalBytesCount = totalBytes,
            CopiedFiles = copiedFiles,
            Duration = duration,
            FailedFiles = 0,
            Errors = Array.Empty<CopyError>()
        };
    }

    /// <summary>
    /// Creates a copy result with some failures
    /// </summary>
    /// <param name="copiedFiles">List of successfully copied files</param>
    /// <param name="duration">Duration of the operation</param>
    /// <param name="failedFiles">Number of files that failed to copy</param>
    /// <param name="errors">List of errors encountered</param>
    /// <returns>CopyResult instance</returns>
    public static CopyResult WithFailures(
        IReadOnlyList<CopiedFile> copiedFiles, 
        TimeSpan duration, 
        int failedFiles,
        IReadOnlyList<CopyError> errors)
    {
        var totalBytes = copiedFiles.Sum(f => f.SizeInBytes);

        return new CopyResult
        {
            FilesCount = copiedFiles.Count,
            TotalBytesCount = totalBytes,
            CopiedFiles = copiedFiles,
            Duration = duration,
            FailedFiles = failedFiles,
            Errors = errors
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB" };
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
/// Information about a file that was successfully copied
/// </summary>
public record CopiedFile
{
    /// <summary>
    /// Original source path of the file
    /// </summary>
    [Required(ErrorMessage = "Source path is required")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Destination path where the file was copied
    /// </summary>
    [Required(ErrorMessage = "Target path is required")]
    public required string TargetPath { get; init; }

    /// <summary>
    /// Size of the copied file in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative")]
    public required long SizeInBytes { get; init; }

    /// <summary>
    /// Timestamp when the file was copied
    /// </summary>
    public DateTime CopiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional duration of copying this specific file
    /// </summary>
    public TimeSpan? CopyDuration { get; init; }

    /// <summary>
    /// Source file name
    /// </summary>
    public string SourceFileName => Path.GetFileName(SourcePath);

    /// <summary>
    /// Target file name
    /// </summary>
    public string TargetFileName => Path.GetFileName(TargetPath);

    /// <summary>
    /// Gets a display-friendly size string
    /// </summary>
    public string DisplaySize => FormatBytes(SizeInBytes);

    /// <summary>
    /// Validates the copied file data
    /// </summary>
    /// <returns>True if the copied file data is valid</returns>
    public bool IsValid()
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(SourcePath) || string.IsNullOrWhiteSpace(TargetPath))
            return false;

        // Check size is non-negative
        if (SizeInBytes < 0)
            return false;

        // Check copy duration if specified
        if (CopyDuration.HasValue && CopyDuration.Value < TimeSpan.Zero)
            return false;

        return true;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB" };
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
/// Represents an error that occurred during a copy operation
/// </summary>
public record CopyError
{
    /// <summary>
    /// Source path of the file that failed to copy
    /// </summary>
    [Required(ErrorMessage = "Source path is required")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Intended target path
    /// </summary>
    [Required(ErrorMessage = "Target path is required")]
    public required string TargetPath { get; init; }

    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    [Required(ErrorMessage = "Error message is required")]
    public required string Message { get; init; }

    /// <summary>
    /// Optional exception type
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a display-friendly error description
    /// </summary>
    public string DisplayMessage => $"{Path.GetFileName(SourcePath)}: {Message}";
}