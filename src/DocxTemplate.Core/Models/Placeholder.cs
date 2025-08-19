using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Represents a placeholder with pattern, locations, and replacement context
/// </summary>
public record Placeholder
{
    /// <summary>
    /// Name of the placeholder (without pattern delimiters)
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Placeholder name must be between 1 and 200 characters")]
    public required string Name { get; init; }

    /// <summary>
    /// Regular expression pattern used to match this placeholder
    /// </summary>
    [Required(ErrorMessage = "Placeholder pattern is required")]
    public required string Pattern { get; init; }

    /// <summary>
    /// All locations where this placeholder appears
    /// </summary>
    [Required(ErrorMessage = "Placeholder locations are required")]
    public required IReadOnlyList<PlaceholderLocation> Locations { get; init; }

    /// <summary>
    /// Total number of occurrences across all files
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total occurrences must be non-negative")]
    public required int TotalOccurrences { get; init; }

    /// <summary>
    /// Constant for the file prefix system placeholder
    /// </summary>
    public const string FilePrefixPlaceholder = "SOUBOR_PREFIX";

    /// <summary>
    /// Validates that the placeholder pattern is a valid regular expression
    /// </summary>
    /// <returns>True if the pattern is valid</returns>
    public bool IsPatternValid()
    {
        try
        {
            var regex = new Regex(Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the number of unique files containing this placeholder
    /// </summary>
    public int UniqueFileCount => Locations.Select(l => l.FilePath).Distinct().Count();

    /// <summary>
    /// Validates the placeholder data integrity
    /// </summary>
    /// <returns>True if the placeholder data is consistent</returns>
    public bool IsValid()
    {
        // Check pattern validity
        if (!IsPatternValid())
            return false;

        // Check that total occurrences matches sum of location occurrences
        var calculatedTotal = Locations.Sum(l => l.Occurrences);
        if (TotalOccurrences != calculatedTotal)
            return false;

        // Check that all locations have valid data
        if (Locations.Any(l => !l.IsValid()))
            return false;

        return true;
    }
}