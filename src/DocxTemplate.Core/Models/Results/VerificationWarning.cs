using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Represents a warning about a placeholder that should have been replaced but was found in the output files
/// </summary>
public record VerificationWarning
{
    /// <summary>
    /// Name of the file where the unreplaced placeholder was found
    /// </summary>
    [Required(ErrorMessage = "File name is required")]
    public required string FileName { get; init; }

    /// <summary>
    /// Name of the placeholder that should have been replaced
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    public required string PlaceholderName { get; init; }

    /// <summary>
    /// The value that should have been used for replacement
    /// </summary>
    [Required(ErrorMessage = "Expected value is required")]
    public required string ExpectedValue { get; init; }

    /// <summary>
    /// Number of times this unreplaced placeholder was found in the file
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Occurrence count must be at least 1")]
    public required int OccurrenceCount { get; init; }

    /// <summary>
    /// Gets a display string for this verification warning
    /// </summary>
    public string DisplayWarning => 
        $"  {PlaceholderName} - mělo být nahrazeno hodnotou \"{ExpectedValue}\" (nalezeno {OccurrenceCount}x)";

    /// <summary>
    /// Creates a verification warning record
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="placeholderName">Name of the placeholder</param>
    /// <param name="expectedValue">The expected replacement value</param>
    /// <param name="occurrenceCount">Number of occurrences</param>
    /// <returns>VerificationWarning instance</returns>
    public static VerificationWarning Create(
        string fileName,
        string placeholderName,
        string expectedValue,
        int occurrenceCount)
    {
        return new VerificationWarning
        {
            FileName = fileName,
            PlaceholderName = placeholderName,
            ExpectedValue = expectedValue,
            OccurrenceCount = occurrenceCount
        };
    }
}