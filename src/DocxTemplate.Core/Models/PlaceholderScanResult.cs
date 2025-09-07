using DocxTemplate.Processing.Models;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Contains discovered placeholders and scan statistics
/// </summary>
public record PlaceholderScanResult
{
    /// <summary>
    /// All unique placeholders discovered during the scan
    /// </summary>
    [Required(ErrorMessage = "Placeholders collection is required")]
    public required IReadOnlyList<Placeholder> Placeholders { get; init; }

    /// <summary>
    /// Total number of files that were scanned
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total files scanned must be non-negative")]
    public required int TotalFilesScanned { get; init; }

    /// <summary>
    /// Duration of the scan operation
    /// </summary>
    public required TimeSpan ScanDuration { get; init; }

    /// <summary>
    /// Number of files that contained at least one placeholder
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files with placeholders must be non-negative")]
    public required int FilesWithPlaceholders { get; init; }

    /// <summary>
    /// Total number of placeholder occurrences found across all files
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total occurrences must be non-negative")]
    public required int TotalOccurrences { get; init; }

    /// <summary>
    /// Number of files that failed to scan due to errors
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Failed files count must be non-negative")]
    public required int FailedFiles { get; init; }

    /// <summary>
    /// List of errors encountered during scanning
    /// </summary>
    public IReadOnlyList<ScanError> Errors { get; init; } = [];

    /// <summary>
    /// Gets the number of unique placeholders found
    /// </summary>
    public int UniquePlaceholderCount => Placeholders.Count;

    /// <summary>
    /// Gets the percentage of files that contained placeholders
    /// </summary>
    public double PlaceholderCoveragePercentage => 
        TotalFilesScanned > 0 ? (FilesWithPlaceholders * 100.0) / TotalFilesScanned : 0;

    /// <summary>
    /// Gets the average number of placeholders per file
    /// </summary>
    public double AveragePlaceholdersPerFile => 
        FilesWithPlaceholders > 0 ? (double)TotalOccurrences / FilesWithPlaceholders : 0;

    /// <summary>
    /// Gets the scan rate in files per second
    /// </summary>
    public double FilesPerSecond => 
        ScanDuration.TotalSeconds > 0 ? TotalFilesScanned / ScanDuration.TotalSeconds : 0;

    /// <summary>
    /// Indicates whether the scan completed successfully
    /// </summary>
    public bool IsSuccessful => FailedFiles == 0 && Errors.Count == 0;

    /// <summary>
    /// Validates the scan result data integrity
    /// </summary>
    /// <returns>True if the scan result data is consistent</returns>
    public bool IsValid()
    {
        // Check that files with placeholders doesn't exceed total files
        if (FilesWithPlaceholders > TotalFilesScanned)
            return false;

        // Check that failed files doesn't exceed total files
        if (FailedFiles > TotalFilesScanned)
            return false;

        // Check that total occurrences matches sum of placeholder occurrences
        var calculatedTotal = Placeholders.Sum(p => p.TotalOccurrences);
        if (TotalOccurrences != calculatedTotal)
            return false;

        // Check that all placeholders are valid
        if (Placeholders.Any(p => !p.IsValid()))
            return false;

        // Check scan duration is non-negative
        if (ScanDuration < TimeSpan.Zero)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a summary string of the scan results
    /// </summary>
    public string GetSummary()
    {
        return $"Scanned {TotalFilesScanned} files in {ScanDuration.TotalMilliseconds:F0}ms. " +
               $"Found {UniquePlaceholderCount} unique placeholders with {TotalOccurrences} total occurrences " +
               $"across {FilesWithPlaceholders} files. " +
               (FailedFiles > 0 ? $"{FailedFiles} files failed to scan. " : "") +
               $"Coverage: {PlaceholderCoveragePercentage:F1}%";
    }

    /// <summary>
    /// Creates a successful scan result
    /// </summary>
    /// <param name="placeholders">Discovered placeholders</param>
    /// <param name="totalFilesScanned">Total files scanned</param>
    /// <param name="scanDuration">Duration of the scan</param>
    /// <param name="filesWithPlaceholders">Number of files containing placeholders</param>
    /// <returns>PlaceholderScanResult instance</returns>
    public static PlaceholderScanResult Success(
        IReadOnlyList<Placeholder> placeholders,
        int totalFilesScanned,
        TimeSpan scanDuration,
        int filesWithPlaceholders)
    {
        var totalOccurrences = placeholders.Sum(p => p.TotalOccurrences);
        
        return new PlaceholderScanResult
        {
            Placeholders = placeholders,
            TotalFilesScanned = totalFilesScanned,
            ScanDuration = scanDuration,
            FilesWithPlaceholders = filesWithPlaceholders,
            TotalOccurrences = totalOccurrences,
            FailedFiles = 0,
            Errors = []
        };
    }

    /// <summary>
    /// Creates a scan result with errors
    /// </summary>
    /// <param name="placeholders">Discovered placeholders</param>
    /// <param name="totalFilesScanned">Total files scanned</param>
    /// <param name="scanDuration">Duration of the scan</param>
    /// <param name="filesWithPlaceholders">Number of files containing placeholders</param>
    /// <param name="failedFiles">Number of files that failed to scan</param>
    /// <param name="errors">List of errors encountered</param>
    /// <returns>PlaceholderScanResult instance</returns>
    public static PlaceholderScanResult WithErrors(
        IReadOnlyList<Placeholder> placeholders,
        int totalFilesScanned,
        TimeSpan scanDuration,
        int filesWithPlaceholders,
        int failedFiles,
        IReadOnlyList<ScanError> errors)
    {
        var totalOccurrences = placeholders.Sum(p => p.TotalOccurrences);
        
        return new PlaceholderScanResult
        {
            Placeholders = placeholders,
            TotalFilesScanned = totalFilesScanned,
            ScanDuration = scanDuration,
            FilesWithPlaceholders = filesWithPlaceholders,
            TotalOccurrences = totalOccurrences,
            FailedFiles = failedFiles,
            Errors = errors
        };
    }
}

/// <summary>
/// Represents an error that occurred during scanning
/// </summary>
public record ScanError
{
    /// <summary>
    /// Path to the file where the error occurred
    /// </summary>
    [Required(ErrorMessage = "File path is required")]
    public required string FilePath { get; init; }

    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    [Required(ErrorMessage = "Error message is required")]
    public required string Message { get; init; }

    /// <summary>
    /// Optional exception details
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a display-friendly error description
    /// </summary>
    public string DisplayMessage => $"{Path.GetFileName(FilePath)}: {Message}";
}