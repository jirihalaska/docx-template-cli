using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Statistics about placeholder usage across multiple files
/// </summary>
public record PlaceholderStatistics
{
    /// <summary>
    /// Total number of unique placeholders found
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Unique placeholder count must be non-negative")]
    public required int UniquePlaceholderCount { get; init; }

    /// <summary>
    /// Total number of placeholder occurrences across all files
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total occurrences must be non-negative")]
    public required int TotalOccurrences { get; init; }

    /// <summary>
    /// Number of files that contain at least one placeholder
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files with placeholders must be non-negative")]
    public required int FilesWithPlaceholders { get; init; }

    /// <summary>
    /// Most frequently used placeholders
    /// </summary>
    [Required(ErrorMessage = "Most frequent placeholders collection is required")]
    public required IReadOnlyList<PlaceholderFrequency> MostFrequentPlaceholders { get; init; }

    /// <summary>
    /// Placeholders that appear in only one file
    /// </summary>
    [Required(ErrorMessage = "Unique to file placeholders collection is required")]
    public required IReadOnlyList<string> UniqueToFilePlaceholders { get; init; }

    /// <summary>
    /// Placeholders that appear in all scanned files
    /// </summary>
    [Required(ErrorMessage = "Universal placeholders collection is required")]
    public required IReadOnlyList<string> UniversalPlaceholders { get; init; }

    /// <summary>
    /// Average number of placeholder occurrences per file
    /// </summary>
    public double AverageOccurrencesPerFile => 
        FilesWithPlaceholders > 0 ? (double)TotalOccurrences / FilesWithPlaceholders : 0;

    /// <summary>
    /// Average number of unique placeholders per file
    /// </summary>
    public double AverageUniquePlaceholdersPerFile { get; init; }

    /// <summary>
    /// Percentage of files that contain placeholders
    /// </summary>
    public double PlaceholderCoveragePercentage { get; init; }

    /// <summary>
    /// Distribution of placeholder usage (how many placeholders appear N times)
    /// </summary>
    [Required(ErrorMessage = "Usage distribution is required")]
    public required IReadOnlyDictionary<int, int> UsageDistribution { get; init; }

    /// <summary>
    /// Gets a summary string of the statistics
    /// </summary>
    public string GetSummary()
    {
        return $"{UniquePlaceholderCount} unique placeholders with {TotalOccurrences} total occurrences " +
               $"across {FilesWithPlaceholders} files. " +
               $"Coverage: {PlaceholderCoveragePercentage:F1}%. " +
               $"Avg: {AverageOccurrencesPerFile:F1} occurrences/file, {AverageUniquePlaceholdersPerFile:F1} unique/file.";
    }

    /// <summary>
    /// Creates placeholder statistics from scan results
    /// </summary>
    /// <param name="scanResult">Scan results to analyze</param>
    /// <param name="totalFilesScanned">Total number of files that were scanned</param>
    /// <returns>PlaceholderStatistics instance</returns>
    public static PlaceholderStatistics FromScanResult(PlaceholderScanResult scanResult, int totalFilesScanned)
    {
        var placeholders = scanResult.Placeholders;
        
        // Calculate most frequent placeholders
        var mostFrequent = placeholders
            .OrderByDescending(p => p.TotalOccurrences)
            .Take(10)
            .Select(p => new PlaceholderFrequency
            {
                PlaceholderName = p.Name,
                Occurrences = p.TotalOccurrences,
                FileCount = p.UniqueFileCount
            })
            .ToList();

        // Find placeholders unique to single files
        var uniqueToFile = placeholders
            .Where(p => p.UniqueFileCount == 1)
            .Select(p => p.Name)
            .ToList();

        // Find universal placeholders (appear in all files with placeholders)
        var universal = placeholders
            .Where(p => p.UniqueFileCount == scanResult.FilesWithPlaceholders)
            .Select(p => p.Name)
            .ToList();

        // Calculate usage distribution
        var usageDistribution = placeholders
            .GroupBy(p => p.TotalOccurrences)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate averages
        var avgUniquePlaceholdersPerFile = scanResult.FilesWithPlaceholders > 0
            ? placeholders.SelectMany(p => p.Locations).GroupBy(l => l.FilePath).Average(g => g.Count())
            : 0;

        var coveragePercentage = totalFilesScanned > 0
            ? (scanResult.FilesWithPlaceholders * 100.0) / totalFilesScanned
            : 0;

        return new PlaceholderStatistics
        {
            UniquePlaceholderCount = placeholders.Count,
            TotalOccurrences = scanResult.TotalOccurrences,
            FilesWithPlaceholders = scanResult.FilesWithPlaceholders,
            MostFrequentPlaceholders = mostFrequent,
            UniqueToFilePlaceholders = uniqueToFile,
            UniversalPlaceholders = universal,
            AverageUniquePlaceholdersPerFile = avgUniquePlaceholdersPerFile,
            PlaceholderCoveragePercentage = coveragePercentage,
            UsageDistribution = usageDistribution
        };
    }
}

/// <summary>
/// Represents the frequency of a placeholder usage
/// </summary>
public record PlaceholderFrequency
{
    /// <summary>
    /// Name of the placeholder
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    public required string PlaceholderName { get; init; }

    /// <summary>
    /// Total number of occurrences
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Occurrences must be non-negative")]
    public required int Occurrences { get; init; }

    /// <summary>
    /// Number of files containing this placeholder
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "File count must be non-negative")]
    public required int FileCount { get; init; }

    /// <summary>
    /// Average occurrences per file containing this placeholder
    /// </summary>
    public double AverageOccurrencesPerFile => 
        FileCount > 0 ? (double)Occurrences / FileCount : 0;

    /// <summary>
    /// Gets a display string for this frequency data
    /// </summary>
    public string DisplayFrequency => 
        $"{PlaceholderName}: {Occurrences} occurrences in {FileCount} file{(FileCount == 1 ? "" : "s")} " +
        $"(avg: {AverageOccurrencesPerFile:F1}/file)";
}