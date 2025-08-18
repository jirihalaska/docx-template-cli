using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Statistical summary of all template sets in a collection
/// </summary>
public record TemplateSetStatistics
{
    /// <summary>
    /// Total number of template sets
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Template set count must be non-negative")]
    public required int TemplateSetCount { get; init; }

    /// <summary>
    /// Total number of template files across all sets
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total template count must be non-negative")]
    public required int TotalTemplateCount { get; init; }

    /// <summary>
    /// Total size of all template files in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total size must be non-negative")]
    public required long TotalSizeBytes { get; init; }

    /// <summary>
    /// Number of template sets that contain subdirectories
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Sets with subfolders must be non-negative")]
    public required int SetsWithSubfolders { get; init; }

    /// <summary>
    /// Largest template set by file count
    /// </summary>
    [Required(ErrorMessage = "Largest set by count is required")]
    public required TemplateSetSummary LargestSetByCount { get; init; }

    /// <summary>
    /// Largest template set by total size
    /// </summary>
    [Required(ErrorMessage = "Largest set by size is required")]
    public required TemplateSetSummary LargestSetBySize { get; init; }

    /// <summary>
    /// Most recently modified template set
    /// </summary>
    [Required(ErrorMessage = "Most recent set is required")]
    public required TemplateSetSummary MostRecentSet { get; init; }

    /// <summary>
    /// Distribution of template counts across sets
    /// </summary>
    [Required(ErrorMessage = "Template count distribution is required")]
    public required IReadOnlyDictionary<string, int> TemplateCountDistribution { get; init; }

    /// <summary>
    /// Common file name patterns found across template sets
    /// </summary>
    [Required(ErrorMessage = "Common patterns collection is required")]
    public required IReadOnlyList<FilePattern> CommonFilePatterns { get; init; }

    /// <summary>
    /// Template sets that appear to be empty or invalid
    /// </summary>
    [Required(ErrorMessage = "Empty sets collection is required")]
    public required IReadOnlyList<string> EmptyOrInvalidSets { get; init; }

    /// <summary>
    /// Timestamp when statistics were generated
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of statistical analysis
    /// </summary>
    public TimeSpan AnalysisDuration { get; init; }

    /// <summary>
    /// Average number of templates per set
    /// </summary>
    public double AverageTemplatesPerSet => 
        TemplateSetCount > 0 ? (double)TotalTemplateCount / TemplateSetCount : 0;

    /// <summary>
    /// Average template set size in bytes
    /// </summary>
    public double AverageSetSizeBytes => 
        TemplateSetCount > 0 ? (double)TotalSizeBytes / TemplateSetCount : 0;

    /// <summary>
    /// Average template file size in bytes
    /// </summary>
    public double AverageTemplateSizeBytes => 
        TotalTemplateCount > 0 ? (double)TotalSizeBytes / TotalTemplateCount : 0;

    /// <summary>
    /// Percentage of template sets that have subdirectories
    /// </summary>
    public double SubfolderPercentage => 
        TemplateSetCount > 0 ? (SetsWithSubfolders * 100.0) / TemplateSetCount : 0;

    /// <summary>
    /// Gets a display-friendly total size string
    /// </summary>
    public string DisplayTotalSize => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Gets a display-friendly average set size string
    /// </summary>
    public string DisplayAverageSetSize => FormatBytes((long)AverageSetSizeBytes);

    /// <summary>
    /// Gets a display-friendly average template size string
    /// </summary>
    public string DisplayAverageTemplateSize => FormatBytes((long)AverageTemplateSizeBytes);

    /// <summary>
    /// Gets a summary string of the statistics
    /// </summary>
    public string GetSummary()
    {
        return $"{TemplateSetCount} template sets with {TotalTemplateCount} total templates ({DisplayTotalSize}). " +
               $"Average: {AverageTemplatesPerSet:F1} templates/set ({DisplayAverageSetSize}/set). " +
               $"Largest: {LargestSetByCount.Name} ({LargestSetByCount.TemplateCount} templates). " +
               $"Structure: {SubfolderPercentage:F1}% have subfolders. " +
               (EmptyOrInvalidSets.Count > 0 ? $"{EmptyOrInvalidSets.Count} empty/invalid sets. " : "") +
               $"Analysis: {AnalysisDuration.TotalMilliseconds:F0}ms";
    }

    /// <summary>
    /// Gets a detailed breakdown of the statistics
    /// </summary>
    public string GetDetailedBreakdown()
    {
        var breakdown = new List<string>
        {
            $"Template Sets: {TemplateSetCount}",
            $"Total Templates: {TotalTemplateCount}",
            $"Total Size: {DisplayTotalSize}",
            $"Average Templates/Set: {AverageTemplatesPerSet:F1}",
            $"Average Set Size: {DisplayAverageSetSize}",
            $"Average Template Size: {DisplayAverageTemplateSize}",
            $"Sets with Subfolders: {SetsWithSubfolders} ({SubfolderPercentage:F1}%)",
            $"Largest by Count: {LargestSetByCount.Name} ({LargestSetByCount.TemplateCount} templates)",
            $"Largest by Size: {LargestSetBySize.Name} ({FormatBytes(LargestSetBySize.TotalSizeBytes)})",
            $"Most Recent: {MostRecentSet.Name} ({MostRecentSet.LastModified:yyyy-MM-dd})"
        };

        if (EmptyOrInvalidSets.Count > 0)
        {
            breakdown.Add($"Empty/Invalid Sets: {string.Join(", ", EmptyOrInvalidSets)}");
        }

        if (CommonFilePatterns.Count > 0)
        {
            breakdown.Add("Common Patterns:");
            foreach (var pattern in CommonFilePatterns.Take(5))
            {
                breakdown.Add($"  {pattern.Pattern}: {pattern.OccurrenceCount} sets ({pattern.Percentage:F1}%)");
            }
        }

        breakdown.Add($"Analysis Duration: {AnalysisDuration.TotalMilliseconds:F0}ms");

        return string.Join("\n", breakdown);
    }

    /// <summary>
    /// Creates template set statistics
    /// </summary>
    /// <param name="templateSets">Template sets to analyze</param>
    /// <param name="analysisDuration">Duration of the analysis</param>
    /// <returns>TemplateSetStatistics instance</returns>
    public static TemplateSetStatistics Create(IReadOnlyList<TemplateSet> templateSets, TimeSpan analysisDuration)
    {
        if (!templateSets.Any())
        {
            return CreateEmpty(analysisDuration);
        }

        var totalTemplateCount = templateSets.Sum(s => s.TemplateCount);
        var totalSizeBytes = templateSets.Sum(s => s.TotalSizeBytes);
        var setsWithSubfolders = templateSets.Count(s => s.HasSubfolders);

        var largestByCount = templateSets.OrderByDescending(s => s.TemplateCount).First();
        var largestBySize = templateSets.OrderByDescending(s => s.TotalSizeBytes).First();
        var mostRecent = templateSets.OrderByDescending(s => s.LastModified).First();

        // Create distribution
        var distribution = new Dictionary<string, int>();
        foreach (var range in new[] { "0", "1-5", "6-10", "11-20", "21-50", "50+" })
        {
            distribution[range] = 0;
        }

        foreach (var set in templateSets)
        {
            var range = set.TemplateCount switch
            {
                0 => "0",
                <= 5 => "1-5",
                <= 10 => "6-10",
                <= 20 => "11-20",
                <= 50 => "21-50",
                _ => "50+"
            };
            distribution[range]++;
        }

        // Find common patterns
        var allFileNames = templateSets.SelectMany(s => s.Templates.Select(t => t.FileName)).ToList();
        var commonPatterns = FindCommonPatterns(allFileNames, templateSets.Count);

        // Find empty or invalid sets
        var emptyOrInvalid = templateSets
            .Where(s => s.TemplateCount == 0 || !s.IsValid())
            .Select(s => s.Name)
            .ToList();

        return new TemplateSetStatistics
        {
            TemplateSetCount = templateSets.Count,
            TotalTemplateCount = totalTemplateCount,
            TotalSizeBytes = totalSizeBytes,
            SetsWithSubfolders = setsWithSubfolders,
            LargestSetByCount = TemplateSetSummary.FromTemplateSet(largestByCount),
            LargestSetBySize = TemplateSetSummary.FromTemplateSet(largestBySize),
            MostRecentSet = TemplateSetSummary.FromTemplateSet(mostRecent),
            TemplateCountDistribution = distribution,
            CommonFilePatterns = commonPatterns,
            EmptyOrInvalidSets = emptyOrInvalid,
            AnalysisDuration = analysisDuration
        };
    }

    private static TemplateSetStatistics CreateEmpty(TimeSpan analysisDuration)
    {
        var emptySet = new TemplateSetSummary
        {
            Name = "None",
            TemplateCount = 0,
            TotalSizeBytes = 0,
            LastModified = DateTime.MinValue
        };

        return new TemplateSetStatistics
        {
            TemplateSetCount = 0,
            TotalTemplateCount = 0,
            TotalSizeBytes = 0,
            SetsWithSubfolders = 0,
            LargestSetByCount = emptySet,
            LargestSetBySize = emptySet,
            MostRecentSet = emptySet,
            TemplateCountDistribution = new Dictionary<string, int>(),
            CommonFilePatterns = [],
            EmptyOrInvalidSets = [],
            AnalysisDuration = analysisDuration
        };
    }

    private static IReadOnlyList<FilePattern> FindCommonPatterns(List<string> fileNames, int totalSets)
    {
        // Simple pattern detection - look for common file extensions and naming patterns
        var patterns = new Dictionary<string, int>();

        // Extension patterns
        var extensions = fileNames.Select(Path.GetExtension).Where(e => !string.IsNullOrEmpty(e));
        foreach (var ext in extensions.GroupBy(e => e))
        {
            patterns[$"*{ext.Key}"] = ext.Count();
        }

        // Common prefixes/suffixes
        var commonWords = new[] { "template", "vzor", "nabidka", "smlouva", "zprava", "zadost" };
        foreach (var word in commonWords)
        {
            var count = fileNames.Count(fn => fn.Contains(word, StringComparison.OrdinalIgnoreCase));
            if (count > 0)
            {
                patterns[$"*{word}*"] = count;
            }
        }

        return patterns
            .Where(kvp => kvp.Value >= Math.Max(1, totalSets / 3)) // Appear in at least 1/3 of sets
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => new FilePattern
            {
                Pattern = kvp.Key,
                OccurrenceCount = kvp.Value,
                Percentage = totalSets > 0 ? (kvp.Value * 100.0) / totalSets : 0
            })
            .ToList();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
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
/// Summary information about a template set
/// </summary>
public record TemplateSetSummary
{
    /// <summary>
    /// Name of the template set
    /// </summary>
    [Required(ErrorMessage = "Template set name is required")]
    public required string Name { get; init; }

    /// <summary>
    /// Number of templates in the set
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Template count must be non-negative")]
    public required int TemplateCount { get; init; }

    /// <summary>
    /// Total size of the template set in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Total size must be non-negative")]
    public required long TotalSizeBytes { get; init; }

    /// <summary>
    /// Last modification time of the template set
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Gets a display-friendly size string
    /// </summary>
    public string DisplaySize => FormatBytes(TotalSizeBytes);

    /// <summary>
    /// Creates a summary from a template set
    /// </summary>
    /// <param name="templateSet">Template set to summarize</param>
    /// <returns>TemplateSetSummary instance</returns>
    public static TemplateSetSummary FromTemplateSet(TemplateSet templateSet)
    {
        return new TemplateSetSummary
        {
            Name = templateSet.Name,
            TemplateCount = templateSet.TemplateCount,
            TotalSizeBytes = templateSet.TotalSizeBytes,
            LastModified = templateSet.LastModified
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
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
/// Represents a common file pattern found across template sets
/// </summary>
public record FilePattern
{
    /// <summary>
    /// The file pattern (e.g., "*.docx", "*template*")
    /// </summary>
    [Required(ErrorMessage = "Pattern is required")]
    public required string Pattern { get; init; }

    /// <summary>
    /// Number of times this pattern occurs
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Occurrence count must be non-negative")]
    public required int OccurrenceCount { get; init; }

    /// <summary>
    /// Percentage of template sets containing this pattern
    /// </summary>
    [Range(0.0, 100.0, ErrorMessage = "Percentage must be between 0 and 100")]
    public required double Percentage { get; init; }

    /// <summary>
    /// Gets a display string for this pattern
    /// </summary>
    public string DisplayPattern => $"{Pattern}: {OccurrenceCount} occurrences ({Percentage:F1}%)";
}