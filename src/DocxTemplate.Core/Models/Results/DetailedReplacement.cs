using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Details about a specific placeholder replacement performed in a file
/// </summary>
public record DetailedReplacement
{
    /// <summary>
    /// Name of the file where the replacement occurred
    /// </summary>
    [Required(ErrorMessage = "File name is required")]
    public required string FileName { get; init; }

    /// <summary>
    /// Name of the placeholder that was replaced
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    public required string PlaceholderName { get; init; }

    /// <summary>
    /// The value that replaced the placeholder
    /// </summary>
    [Required(ErrorMessage = "Replaced value is required")]
    public required string ReplacedValue { get; init; }

    /// <summary>
    /// Number of times this replacement occurred in the file
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Occurrence count must be at least 1")]
    public required int OccurrenceCount { get; init; }

    /// <summary>
    /// Gets a display string for this replacement detail
    /// </summary>
    public string DisplayReplacement => 
        $"  {PlaceholderName} → \"{ReplacedValue}\" ({OccurrenceCount}x)";

    /// <summary>
    /// Creates a detailed replacement record
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="placeholderName">Name of the placeholder</param>
    /// <param name="replacedValue">The replacement value</param>
    /// <param name="occurrenceCount">Number of occurrences</param>
    /// <returns>DetailedReplacement instance</returns>
    public static DetailedReplacement Create(
        string fileName,
        string placeholderName,
        string replacedValue,
        int occurrenceCount)
    {
        return new DetailedReplacement
        {
            FileName = fileName,
            PlaceholderName = placeholderName,
            ReplacedValue = replacedValue,
            OccurrenceCount = occurrenceCount
        };
    }
}