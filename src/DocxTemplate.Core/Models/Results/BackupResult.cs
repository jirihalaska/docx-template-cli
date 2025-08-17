using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Result of a backup operation
/// </summary>
public record BackupResult
{
    /// <summary>
    /// Number of files successfully backed up
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files backed up must be non-negative")]
    public required int FilesBackedUp { get; init; }

    /// <summary>
    /// Total size of backed up files in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total bytes backed up must be non-negative")]
    public required long TotalBytesBackedUp { get; init; }

    /// <summary>
    /// Details of all files that were backed up
    /// </summary>
    [Required(ErrorMessage = "Backup details collection is required")]
    public required IReadOnlyList<BackupDetail> BackupDetails { get; init; }

    /// <summary>
    /// Duration of the backup operation
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Directory where backups were stored
    /// </summary>
    [Required(ErrorMessage = "Backup directory is required")]
    public required string BackupDirectory { get; init; }

    /// <summary>
    /// Number of files that failed to backup
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Failed files count must be non-negative")]
    public required int FailedFiles { get; init; }

    /// <summary>
    /// List of errors encountered during backup
    /// </summary>
    public IReadOnlyList<BackupError> Errors { get; init; } = Array.Empty<BackupError>();

    /// <summary>
    /// Total number of files attempted (successful + failed)
    /// </summary>
    public int TotalFilesAttempted => FilesBackedUp + FailedFiles;

    /// <summary>
    /// Backup rate in files per second
    /// </summary>
    public double FilesPerSecond => 
        Duration.TotalSeconds > 0 ? FilesBackedUp / Duration.TotalSeconds : 0;

    /// <summary>
    /// Backup rate in bytes per second
    /// </summary>
    public double BytesPerSecond => 
        Duration.TotalSeconds > 0 ? TotalBytesBackedUp / Duration.TotalSeconds : 0;

    /// <summary>
    /// Success rate as a percentage
    /// </summary>
    public double SuccessRatePercentage => 
        TotalFilesAttempted > 0 ? (FilesBackedUp * 100.0) / TotalFilesAttempted : 100.0;

    /// <summary>
    /// Indicates whether the operation was completely successful
    /// </summary>
    public bool IsCompletelySuccessful => FailedFiles == 0 && Errors.Count == 0;

    /// <summary>
    /// Average file size in bytes
    /// </summary>
    public double AverageFileSize => 
        FilesBackedUp > 0 ? (double)TotalBytesBackedUp / FilesBackedUp : 0;

    /// <summary>
    /// Gets a display-friendly total size string
    /// </summary>
    public string DisplayTotalSize => FormatBytes(TotalBytesBackedUp);

    /// <summary>
    /// Gets a display-friendly throughput string
    /// </summary>
    public string DisplayThroughput => $"{FormatBytes((long)BytesPerSecond)}/s";

    /// <summary>
    /// Gets a summary string of the backup results
    /// </summary>
    public string GetSummary()
    {
        return $"Backed up {FilesBackedUp} files ({DisplayTotalSize}) in {Duration.TotalMilliseconds:F0}ms " +
               $"to '{BackupDirectory}'. " +
               (FailedFiles > 0 ? $"{FailedFiles} files failed. " : "") +
               $"Rate: {FilesPerSecond:F1} files/sec ({DisplayThroughput}). " +
               $"Success rate: {SuccessRatePercentage:F1}%";
    }

    /// <summary>
    /// Creates a successful backup result
    /// </summary>
    /// <param name="backupDetails">Details of backed up files</param>
    /// <param name="backupDirectory">Directory where backups were stored</param>
    /// <param name="duration">Duration of the operation</param>
    /// <returns>BackupResult instance</returns>
    public static BackupResult Success(
        IReadOnlyList<BackupDetail> backupDetails,
        string backupDirectory,
        TimeSpan duration)
    {
        var totalBytes = backupDetails.Sum(d => d.SizeBytes);

        return new BackupResult
        {
            FilesBackedUp = backupDetails.Count,
            TotalBytesBackedUp = totalBytes,
            BackupDetails = backupDetails,
            Duration = duration,
            BackupDirectory = backupDirectory,
            FailedFiles = 0,
            Errors = Array.Empty<BackupError>()
        };
    }

    /// <summary>
    /// Creates a backup result with some failures
    /// </summary>
    /// <param name="backupDetails">Details of successfully backed up files</param>
    /// <param name="backupDirectory">Directory where backups were stored</param>
    /// <param name="duration">Duration of the operation</param>
    /// <param name="failedFiles">Number of files that failed to backup</param>
    /// <param name="errors">List of errors encountered</param>
    /// <returns>BackupResult instance</returns>
    public static BackupResult WithFailures(
        IReadOnlyList<BackupDetail> backupDetails,
        string backupDirectory,
        TimeSpan duration,
        int failedFiles,
        IReadOnlyList<BackupError> errors)
    {
        var totalBytes = backupDetails.Sum(d => d.SizeBytes);

        return new BackupResult
        {
            FilesBackedUp = backupDetails.Count,
            TotalBytesBackedUp = totalBytes,
            BackupDetails = backupDetails,
            Duration = duration,
            BackupDirectory = backupDirectory,
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
/// Details of a single file backup operation
/// </summary>
public record BackupDetail
{
    /// <summary>
    /// Original source path of the file
    /// </summary>
    [Required(ErrorMessage = "Source path is required")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Path where the backup was created
    /// </summary>
    [Required(ErrorMessage = "Backup path is required")]
    public required string BackupPath { get; init; }

    /// <summary>
    /// Size of the backed up file in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative")]
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Timestamp when the backup was created
    /// </summary>
    public DateTime BackupCreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of backing up this specific file
    /// </summary>
    public TimeSpan? BackupDuration { get; init; }

    /// <summary>
    /// Checksum or hash of the backup file for verification
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Type of backup that was performed
    /// </summary>
    public BackupType BackupType { get; init; } = BackupType.Copy;

    /// <summary>
    /// Source file name
    /// </summary>
    public string SourceFileName => Path.GetFileName(SourcePath);

    /// <summary>
    /// Backup file name
    /// </summary>
    public string BackupFileName => Path.GetFileName(BackupPath);

    /// <summary>
    /// Gets a display-friendly size string
    /// </summary>
    public string DisplaySize => FormatBytes(SizeBytes);

    /// <summary>
    /// Indicates whether the backup can be verified (has checksum)
    /// </summary>
    public bool CanVerify => !string.IsNullOrEmpty(Checksum);

    /// <summary>
    /// Gets a display string for this backup detail
    /// </summary>
    public string DisplayBackup => 
        $"{SourceFileName} → {BackupFileName} ({DisplaySize})";

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
/// Represents an error that occurred during backup
/// </summary>
public record BackupError
{
    /// <summary>
    /// Source path of the file that failed to backup
    /// </summary>
    [Required(ErrorMessage = "Source path is required")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Intended backup path
    /// </summary>
    [Required(ErrorMessage = "Backup path is required")]
    public required string BackupPath { get; init; }

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

/// <summary>
/// Types of backup operations
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Simple file copy backup
    /// </summary>
    Copy,

    /// <summary>
    /// Compressed backup
    /// </summary>
    Compressed,

    /// <summary>
    /// Incremental backup (only changes)
    /// </summary>
    Incremental,

    /// <summary>
    /// Differential backup
    /// </summary>
    Differential
}