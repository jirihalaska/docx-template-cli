using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Tracks where placeholders appear in templates with position tracking and context information
/// </summary>
public record PlaceholderLocation
{
    /// <summary>
    /// Name of the file containing the placeholder
    /// </summary>
    [Required(ErrorMessage = "File name is required")]
    public required string FileName { get; init; }

    /// <summary>
    /// Full path to the file containing the placeholder
    /// </summary>
    [Required(ErrorMessage = "File path is required")]
    public required string FilePath { get; init; }

    /// <summary>
    /// Number of times the placeholder appears in this file
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Occurrences must be at least 1")]
    public required int Occurrences { get; init; }

    /// <summary>
    /// Optional context information about where the placeholder appears
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Optional line numbers where the placeholder appears (if available)
    /// </summary>
    public IReadOnlyList<int>? LineNumbers { get; init; }

    /// <summary>
    /// Optional character positions where the placeholder appears (if available)
    /// </summary>
    public IReadOnlyList<int>? CharacterPositions { get; init; }
    
    /// <summary>
    /// Original placeholder syntax as found in the document
    /// </summary>
    public string? OriginalSyntax { get; init; }
    
    /// <summary>
    /// Metadata specific to the placeholder type (e.g., image dimensions)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Validates the location data
    /// </summary>
    /// <returns>True if the location data is valid</returns>
    public bool IsValid()
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(FileName) || string.IsNullOrWhiteSpace(FilePath))
            return false;

        // Check that occurrences is positive
        if (Occurrences <= 0)
            return false;

        // If line numbers are provided, they should match occurrences count
        if (LineNumbers != null && LineNumbers.Count != Occurrences)
            return false;

        // If character positions are provided, they should match occurrences count
        if (CharacterPositions != null && CharacterPositions.Count != Occurrences)
            return false;

        // Line numbers should be positive
        if (LineNumbers != null && LineNumbers.Any(ln => ln <= 0))
            return false;

        // Character positions should be non-negative
        if (CharacterPositions != null && CharacterPositions.Any(cp => cp < 0))
            return false;

        return true;
    }

    /// <summary>
    /// Gets a display string for this location
    /// </summary>
    public string DisplayLocation
    {
        get
        {
            var result = $"{FileName} ({Occurrences} occurrence{(Occurrences == 1 ? "" : "s")})";
            
            if (LineNumbers?.Any() == true)
            {
                result += $" at line{(LineNumbers.Count == 1 ? "" : "s")} {string.Join(", ", LineNumbers)}";
            }
            
            return result;
        }
    }
}