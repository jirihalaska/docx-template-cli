using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Result of a placeholder replacement operation
/// </summary>
public record ReplaceResult
{
    /// <summary>
    /// Number of files that were processed
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files processed must be non-negative")]
    public required int FilesProcessed { get; init; }

    /// <summary>
    /// Total number of placeholder replacements made across all files
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total replacements must be non-negative")]
    public required int TotalReplacements { get; init; }

    /// <summary>
    /// Detailed results for each file that was processed
    /// </summary>
    [Required(ErrorMessage = "File results are required")]
    public required IReadOnlyList<FileReplaceResult> FileResults { get; init; }

    /// <summary>
    /// Duration of the replacement operation
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Indicates whether any errors occurred during processing
    /// </summary>
    public required bool HasErrors { get; init; }

    /// <summary>
    /// Number of files that were successfully processed
    /// </summary>
    public int SuccessfulFiles => FileResults.Count(r => r.IsSuccess);

    /// <summary>
    /// Number of files that failed to process
    /// </summary>
    public int FailedFiles => FileResults.Count(r => !r.IsSuccess);

    /// <summary>
    /// Average number of replacements per successfully processed file
    /// </summary>
    public double AverageReplacementsPerFile => 
        SuccessfulFiles > 0 ? (double)TotalReplacements / SuccessfulFiles : 0;

    /// <summary>
    /// Processing rate in files per second
    /// </summary>
    public double FilesPerSecond => 
        Duration.TotalSeconds > 0 ? FilesProcessed / Duration.TotalSeconds : 0;

    /// <summary>
    /// Indicates whether the operation was completely successful
    /// </summary>
    public bool IsCompletelySuccessful => !HasErrors && FailedFiles == 0;

    /// <summary>
    /// Gets all error messages from failed file operations
    /// </summary>
    public IEnumerable<string> AllErrors => 
        FileResults.Where(r => !r.IsSuccess && !string.IsNullOrEmpty(r.ErrorMessage))
                   .Select(r => r.ErrorMessage!);

    /// <summary>
    /// Validates the replace result data integrity
    /// </summary>
    /// <returns>True if the result data is consistent</returns>
    public bool IsValid()
    {
        // Check that total replacements matches sum of file replacements
        var calculatedTotal = FileResults.Sum(r => r.ReplacementCount);
        if (TotalReplacements != calculatedTotal)
            return false;

        // Check that files processed matches file results count
        if (FilesProcessed != FileResults.Count)
            return false;

        // Check that HasErrors is consistent with file results
        var hasActualErrors = FileResults.Any(r => !r.IsSuccess);
        if (HasErrors != hasActualErrors)
            return false;

        // Check duration is non-negative
        if (Duration < TimeSpan.Zero)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a summary string of the replacement results
    /// </summary>
    public string GetSummary()
    {
        return $"Processed {FilesProcessed} files in {Duration.TotalMilliseconds:F0}ms. " +
               $"Made {TotalReplacements} replacements across {SuccessfulFiles} files. " +
               (FailedFiles > 0 ? $"{FailedFiles} files failed. " : "") +
               $"Rate: {FilesPerSecond:F1} files/sec";
    }

    /// <summary>
    /// Creates a successful replace result
    /// </summary>
    /// <param name="fileResults">Results for individual files</param>
    /// <param name="duration">Duration of the operation</param>
    /// <returns>ReplaceResult instance</returns>
    public static ReplaceResult Success(IReadOnlyList<FileReplaceResult> fileResults, TimeSpan duration)
    {
        var totalReplacements = fileResults.Sum(r => r.ReplacementCount);
        var hasErrors = fileResults.Any(r => !r.IsSuccess);

        return new ReplaceResult
        {
            FilesProcessed = fileResults.Count,
            TotalReplacements = totalReplacements,
            FileResults = fileResults,
            Duration = duration,
            HasErrors = hasErrors
        };
    }
}

/// <summary>
/// Result of replacing placeholders in a single file
/// </summary>
public record FileReplaceResult
{
    /// <summary>
    /// Full path to the file that was processed
    /// </summary>
    [Required(ErrorMessage = "File path is required")]
    public required string FilePath { get; init; }

    /// <summary>
    /// Number of placeholder replacements made in this file
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Replacement count must be non-negative")]
    public required int ReplacementCount { get; init; }

    /// <summary>
    /// Indicates whether the file was processed successfully
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Path to the backup file if one was created
    /// </summary>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Duration of processing this specific file
    /// </summary>
    public TimeSpan? ProcessingDuration { get; init; }

    /// <summary>
    /// Size of the file after processing
    /// </summary>
    public long? FinalSizeBytes { get; init; }

    /// <summary>
    /// Detailed information about each placeholder replacement performed in this file
    /// </summary>
    public IReadOnlyList<DetailedReplacement> DetailedReplacements { get; init; } = Array.Empty<DetailedReplacement>();

    /// <summary>
    /// Information about placeholders that were found but not replaced because no value was provided
    /// </summary>
    public IReadOnlyList<UnreplacedPlaceholder> UnreplacedPlaceholders { get; init; } = Array.Empty<UnreplacedPlaceholder>();

    /// <summary>
    /// File name without path
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Indicates whether a backup was created
    /// </summary>
    public bool HasBackup => !string.IsNullOrEmpty(BackupPath);

    /// <summary>
    /// Gets a display-friendly result description
    /// </summary>
    public string DisplayResult
    {
        get
        {
            if (IsSuccess)
            {
                var result = $"{FileName}: {ReplacementCount} replacement{(ReplacementCount == 1 ? "" : "s")}";
                if (HasBackup)
                    result += " (backup created)";
                return result;
            }
            else
            {
                return $"{FileName}: Failed - {ErrorMessage ?? "Unknown error"}";
            }
        }
    }

    /// <summary>
    /// Creates a successful file replace result
    /// </summary>
    /// <param name="filePath">Path to the processed file</param>
    /// <param name="replacementCount">Number of replacements made</param>
    /// <param name="backupPath">Optional backup file path</param>
    /// <param name="processingDuration">Optional processing duration</param>
    /// <param name="finalSizeBytes">Optional final file size</param>
    /// <param name="detailedReplacements">Optional detailed replacement information</param>
    /// <param name="unreplacedPlaceholders">Optional unreplaced placeholder information</param>
    /// <returns>FileReplaceResult instance</returns>
    public static FileReplaceResult Success(
        string filePath, 
        int replacementCount, 
        string? backupPath = null,
        TimeSpan? processingDuration = null,
        long? finalSizeBytes = null,
        IReadOnlyList<DetailedReplacement>? detailedReplacements = null,
        IReadOnlyList<UnreplacedPlaceholder>? unreplacedPlaceholders = null)
    {
        return new FileReplaceResult
        {
            FilePath = filePath,
            ReplacementCount = replacementCount,
            IsSuccess = true,
            BackupPath = backupPath,
            ProcessingDuration = processingDuration,
            FinalSizeBytes = finalSizeBytes,
            DetailedReplacements = detailedReplacements ?? Array.Empty<DetailedReplacement>(),
            UnreplacedPlaceholders = unreplacedPlaceholders ?? Array.Empty<UnreplacedPlaceholder>()
        };
    }

    /// <summary>
    /// Creates a failed file replace result
    /// </summary>
    /// <param name="filePath">Path to the file that failed</param>
    /// <param name="errorMessage">Error message describing the failure</param>
    /// <param name="processingDuration">Optional processing duration before failure</param>
    /// <returns>FileReplaceResult instance</returns>
    public static FileReplaceResult Failure(
        string filePath, 
        string errorMessage,
        TimeSpan? processingDuration = null)
    {
        return new FileReplaceResult
        {
            FilePath = filePath,
            ReplacementCount = 0,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ProcessingDuration = processingDuration
        };
    }
}