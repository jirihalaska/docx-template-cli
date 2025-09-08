using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Details about a placeholder that was found but not replaced because no value was provided
/// </summary>
public record UnreplacedPlaceholder
{
    /// <summary>
    /// Name of the file where the unreplaced placeholder was found
    /// </summary>
    [Required(ErrorMessage = "File name is required")]
    public required string FileName { get; init; }

    /// <summary>
    /// Name of the placeholder that was not replaced
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    public required string PlaceholderName { get; init; }

    /// <summary>
    /// Number of times this placeholder occurred in the file
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Occurrence count must be at least 1")]
    public required int OccurrenceCount { get; init; }

    /// <summary>
    /// Gets a display string for this unreplaced placeholder
    /// </summary>
    public string DisplayUnreplaced => 
        $"  {PlaceholderName} (nezpracováno, {OccurrenceCount}x)";

    /// <summary>
    /// Creates an unreplaced placeholder record
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="placeholderName">Name of the placeholder</param>
    /// <param name="occurrenceCount">Number of occurrences</param>
    /// <returns>UnreplacedPlaceholder instance</returns>
    public static UnreplacedPlaceholder Create(
        string fileName,
        string placeholderName,
        int occurrenceCount)
    {
        return new UnreplacedPlaceholder
        {
            FileName = fileName,
            PlaceholderName = placeholderName,
            OccurrenceCount = occurrenceCount
        };
    }
}