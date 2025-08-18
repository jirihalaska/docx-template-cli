using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Statistical analysis of placeholder usage across multiple files
/// </summary>
public record PlaceholderStatistics
{
    /// <summary>
    /// Total number of unique placeholders found
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total unique placeholders must be non-negative")]
    public required int TotalUniquePlaceholders { get; init; }

    /// <summary>
    /// Total number of placeholder occurrences across all files
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total occurrences must be non-negative")]
    public required int TotalOccurrences { get; init; }

    /// <summary>
    /// Number of files that were scanned
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files scanned must be non-negative")]
    public required int FilesScanned { get; init; }

    /// <summary>
    /// Number of files that contained at least one placeholder
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files with placeholders must be non-negative")]
    public required int FilesWithPlaceholders { get; init; }

    /// <summary>
    /// Average number of placeholders per file that contains placeholders
    /// </summary>
    public required double AveragePerFile { get; init; }

    /// <summary>
    /// List of most commonly used placeholders (top 10)
    /// </summary>
    [Required(ErrorMessage = "Most common placeholders list is required")]
    public required IReadOnlyList<Placeholder> MostCommonPlaceholders { get; init; }

    /// <summary>
    /// Distribution of placeholder counts per file
    /// </summary>
    [Required(ErrorMessage = "Placeholder distribution is required")]
    public required IReadOnlyDictionary<int, int> PlaceholderDistribution { get; init; }

    /// <summary>
    /// Duration of the scan operation
    /// </summary>
    public required TimeSpan ScanDuration { get; init; }

    /// <summary>
    /// Gets the coverage percentage (files with placeholders / total files)
    /// </summary>
    public double CoveragePercentage => 
        FilesScanned > 0 ? (FilesWithPlaceholders * 100.0) / FilesScanned : 0;

    /// <summary>
    /// Gets the scan rate in files per second
    /// </summary>
    public double FilesPerSecond => 
        ScanDuration.TotalSeconds > 0 ? FilesScanned / ScanDuration.TotalSeconds : 0;

    /// <summary>
    /// Gets the average occurrences per unique placeholder
    /// </summary>
    public double AverageOccurrencesPerPlaceholder => 
        TotalUniquePlaceholders > 0 ? (double)TotalOccurrences / TotalUniquePlaceholders : 0;

    /// <summary>
    /// Gets a summary string of the statistics
    /// </summary>
    public string GetSummary()
    {
        return $"Found {TotalUniquePlaceholders} unique placeholders with {TotalOccurrences} total occurrences " +
               $"across {FilesWithPlaceholders} of {FilesScanned} files ({CoveragePercentage:F1}% coverage). " +
               $"Scan completed in {ScanDuration.TotalMilliseconds:F0}ms " +
               $"({FilesPerSecond:F1} files/sec).";
    }

    /// <summary>
    /// Gets the most frequent placeholder
    /// </summary>
    public Placeholder? MostFrequentPlaceholder => 
        MostCommonPlaceholders.FirstOrDefault();

    /// <summary>
    /// Gets the least frequent placeholder from the most common list
    /// </summary>
    public Placeholder? LeastFrequentFromTop => 
        MostCommonPlaceholders.LastOrDefault();

    /// <summary>
    /// Validates the statistics data integrity
    /// </summary>
    /// <returns>True if the statistics data is consistent</returns>
    public bool IsValid()
    {
        // Check that files with placeholders doesn't exceed total files
        if (FilesWithPlaceholders > FilesScanned)
            return false;

        // Check that most common placeholders list is reasonable
        if (MostCommonPlaceholders.Count > TotalUniquePlaceholders)
            return false;

        // Check that average is reasonable
        if (AveragePerFile < 0)
            return false;

        // Check that scan duration is non-negative
        if (ScanDuration < TimeSpan.Zero)
            return false;

        // Check that all placeholders in most common list are valid
        if (MostCommonPlaceholders.Any(p => !p.IsValid()))
            return false;

        return true;
    }

    /// <summary>
    /// Creates empty statistics for when no placeholders are found
    /// </summary>
    /// <param name="filesScanned">Number of files scanned</param>
    /// <param name="scanDuration">Duration of the scan</param>
    /// <returns>Empty PlaceholderStatistics instance</returns>
    public static PlaceholderStatistics Empty(int filesScanned, TimeSpan scanDuration)
    {
        return new PlaceholderStatistics
        {
            TotalUniquePlaceholders = 0,
            TotalOccurrences = 0,
            FilesScanned = filesScanned,
            FilesWithPlaceholders = 0,
            AveragePerFile = 0,
            MostCommonPlaceholders = [],
            PlaceholderDistribution = new Dictionary<int, int>(),
            ScanDuration = scanDuration
        };
    }
}