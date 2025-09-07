using DocxTemplate.Processing.Models;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Preview of what replacements would be made without actually modifying files
/// </summary>
public record ReplacementPreview
{
    /// <summary>
    /// Number of files that would be processed
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Files to process must be non-negative")]
    public required int FilesToProcess { get; init; }

    /// <summary>
    /// Total number of replacements that would be made
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total replacements must be non-negative")]
    public required int TotalReplacements { get; init; }

    /// <summary>
    /// Preview details for each file
    /// </summary>
    [Required(ErrorMessage = "File previews collection is required")]
    public required IReadOnlyList<FileReplacementPreview> FilePreviews { get; init; }

    /// <summary>
    /// Placeholders that have replacement values
    /// </summary>
    [Required(ErrorMessage = "Mapped placeholders collection is required")]
    public required IReadOnlyList<string> MappedPlaceholders { get; init; }

    /// <summary>
    /// Placeholders that don't have replacement values
    /// </summary>
    [Required(ErrorMessage = "Unmapped placeholders collection is required")]
    public required IReadOnlyList<string> UnmappedPlaceholders { get; init; }

    /// <summary>
    /// Replacement mappings that don't match any placeholders
    /// </summary>
    [Required(ErrorMessage = "Unused mappings collection is required")]
    public required IReadOnlyList<string> UnusedMappings { get; init; }

    /// <summary>
    /// Duration of the preview operation
    /// </summary>
    public TimeSpan PreviewDuration { get; init; }

    /// <summary>
    /// Number of files that would have successful replacements
    /// </summary>
    public int SuccessfulFiles => FilePreviews.Count(f => f.CanProcess);

    /// <summary>
    /// Number of files that would fail processing
    /// </summary>
    public int FailedFiles => FilePreviews.Count(f => !f.CanProcess);

    /// <summary>
    /// Percentage of placeholders that have mappings
    /// </summary>
    public double MappingCoveragePercentage
    {
        get
        {
            var totalPlaceholders = MappedPlaceholders.Count + UnmappedPlaceholders.Count;
            return totalPlaceholders > 0 ? (MappedPlaceholders.Count * 100.0) / totalPlaceholders : 100.0;
        }
    }

    /// <summary>
    /// Average replacements per file
    /// </summary>
    public double AverageReplacementsPerFile => 
        FilesToProcess > 0 ? (double)TotalReplacements / FilesToProcess : 0;

    /// <summary>
    /// Gets a summary string of the preview
    /// </summary>
    public string GetSummary()
    {
        return $"Preview: {TotalReplacements} replacements across {FilesToProcess} files. " +
               $"Mapping coverage: {MappingCoveragePercentage:F1}% " +
               $"({MappedPlaceholders.Count}/{MappedPlaceholders.Count + UnmappedPlaceholders.Count} placeholders). " +
               (UnmappedPlaceholders.Count > 0 ? $"{UnmappedPlaceholders.Count} unmapped placeholders. " : "") +
               (UnusedMappings.Count > 0 ? $"{UnusedMappings.Count} unused mappings. " : "") +
               (FailedFiles > 0 ? $"{FailedFiles} files would fail. " : "");
    }

    /// <summary>
    /// Creates a replacement preview
    /// </summary>
    /// <param name="filePreviews">Preview details for each file</param>
    /// <param name="mappedPlaceholders">Placeholders with mappings</param>
    /// <param name="unmappedPlaceholders">Placeholders without mappings</param>
    /// <param name="unusedMappings">Mappings that don't match placeholders</param>
    /// <param name="previewDuration">Duration of the preview operation</param>
    /// <returns>ReplacementPreview instance</returns>
    public static ReplacementPreview Create(
        IReadOnlyList<FileReplacementPreview> filePreviews,
        IReadOnlyList<string> mappedPlaceholders,
        IReadOnlyList<string> unmappedPlaceholders,
        IReadOnlyList<string> unusedMappings,
        TimeSpan previewDuration)
    {
        var totalReplacements = filePreviews.Sum(f => f.ReplacementCount);

        return new ReplacementPreview
        {
            FilesToProcess = filePreviews.Count,
            TotalReplacements = totalReplacements,
            FilePreviews = filePreviews,
            MappedPlaceholders = mappedPlaceholders,
            UnmappedPlaceholders = unmappedPlaceholders,
            UnusedMappings = unusedMappings,
            PreviewDuration = previewDuration
        };
    }
}

/// <summary>
/// Preview of replacements for a single file
/// </summary>
public record FileReplacementPreview
{
    /// <summary>
    /// Path to the file
    /// </summary>
    [Required(ErrorMessage = "File path is required")]
    public required string FilePath { get; init; }

    /// <summary>
    /// Number of replacements that would be made in this file
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Replacement count must be non-negative")]
    public required int ReplacementCount { get; init; }

    /// <summary>
    /// Whether the file can be processed successfully
    /// </summary>
    public required bool CanProcess { get; init; }

    /// <summary>
    /// Error message if the file cannot be processed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Details of each replacement that would be made
    /// </summary>
    [Required(ErrorMessage = "Replacement details collection is required")]
    public required IReadOnlyList<ReplacementDetail> ReplacementDetails { get; init; }

    /// <summary>
    /// Estimated processing time for this file
    /// </summary>
    public TimeSpan? EstimatedProcessingTime { get; init; }

    /// <summary>
    /// Current file size in bytes
    /// </summary>
    public long CurrentSizeBytes { get; init; }

    /// <summary>
    /// Estimated file size after replacements
    /// </summary>
    public long EstimatedSizeBytes { get; init; }

    /// <summary>
    /// File name without path
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Change in file size (positive = larger, negative = smaller)
    /// </summary>
    public long SizeChange => EstimatedSizeBytes - CurrentSizeBytes;

    /// <summary>
    /// Gets a display string for this file preview
    /// </summary>
    public string DisplayPreview
    {
        get
        {
            if (!CanProcess)
                return $"{FileName}: Cannot process - {ErrorMessage ?? "Unknown error"}";

            var sizeChangeDisplay = SizeChange == 0 ? "no size change" :
                SizeChange > 0 ? $"+{FormatBytes(SizeChange)}" : 
                $"-{FormatBytes(-SizeChange)}";

            return $"{FileName}: {ReplacementCount} replacement{(ReplacementCount == 1 ? "" : "s")} ({sizeChangeDisplay})";
        }
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
/// Details of a specific replacement that would be made
/// </summary>
public record ReplacementDetail
{
    /// <summary>
    /// Name of the placeholder being replaced
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    public required string PlaceholderName { get; init; }

    /// <summary>
    /// Current value (placeholder text) that would be replaced
    /// </summary>
    [Required(ErrorMessage = "Current value is required")]
    public required string CurrentValue { get; init; }

    /// <summary>
    /// New value that would replace the placeholder
    /// </summary>
    [Required(ErrorMessage = "New value is required")]
    public required string NewValue { get; init; }

    /// <summary>
    /// Number of times this replacement would occur in the file
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Occurrence count must be at least 1")]
    public required int OccurrenceCount { get; init; }

    /// <summary>
    /// Optional context where the replacement would occur
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Change in character length for this replacement
    /// </summary>
    public int LengthChange => (NewValue.Length - CurrentValue.Length) * OccurrenceCount;

    /// <summary>
    /// Gets a display string for this replacement detail
    /// </summary>
    public string DisplayReplacement => 
        $"{PlaceholderName}: '{CurrentValue}' → '{NewValue}' ({OccurrenceCount}x)";
}