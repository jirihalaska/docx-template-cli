using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DocxTemplate.Core.Models;

/// <summary>
/// Represents a mapping of placeholders to replacement values with validation
/// </summary>
public record ReplacementMap
{
    /// <summary>
    /// Dictionary mapping placeholder names to replacement values
    /// </summary>
    [Required(ErrorMessage = "Replacement mappings are required")]
    public required IReadOnlyDictionary<string, string> Mappings { get; init; }

    /// <summary>
    /// Optional source file path where the replacement map was loaded from
    /// </summary>
    public string? SourceFilePath { get; init; }

    /// <summary>
    /// Timestamp when the replacement map was created or loaded
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the number of replacement mappings
    /// </summary>
    public int Count => Mappings.Count;

    /// <summary>
    /// Gets all placeholder names in the map
    /// </summary>
    public IEnumerable<string> PlaceholderNames => Mappings.Keys;

    /// <summary>
    /// Gets all replacement values in the map
    /// </summary>
    public IEnumerable<string> ReplacementValues => Mappings.Values;

    /// <summary>
    /// Validates the replacement map data
    /// </summary>
    /// <returns>True if the replacement map is valid</returns>
    public bool IsValid()
    {
        // Check that mappings is not null
        if (Mappings == null)
            return false;

        // Check that all keys are valid (non-null, non-empty)
        if (Mappings.Keys.Any(k => string.IsNullOrWhiteSpace(k)))
            return false;

        // Check that all values are non-null (empty strings are allowed)
        if (Mappings.Values.Any(v => v == null))
            return false;

        // Check for duplicate keys (case-insensitive)
        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Mappings.Keys)
        {
            if (!uniqueKeys.Add(key))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a placeholder name according to business rules
    /// </summary>
    /// <param name="placeholderName">Name to validate</param>
    /// <returns>True if the placeholder name is valid</returns>
    public static bool IsValidPlaceholderName(string placeholderName)
    {
        if (string.IsNullOrWhiteSpace(placeholderName))
            return false;

        // Check length constraints
        if (placeholderName.Length > 200)
            return false;

        // Check for invalid characters (control characters, etc.)
        if (placeholderName.Any(c => char.IsControl(c) && c != '\t'))
            return false;

        return true;
    }

    /// <summary>
    /// Sanitizes replacement value to ensure it's safe for document insertion
    /// </summary>
    /// <param name="value">Value to sanitize</param>
    /// <returns>Sanitized value</returns>
    public static string SanitizeReplacementValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Remove or replace potentially dangerous characters
        var sanitized = value
            .Replace('\0', ' ') // Remove null characters
            .Replace('\t', ' ') // Replace tab with space
            .Replace('\x1F', ' ') // Remove unit separator
            .Replace('\x1E', ' ') // Remove record separator
            .Replace('\x1D', ' ') // Remove group separator
            .Replace('\x1C', ' '); // Remove file separator

        return sanitized;
    }

    /// <summary>
    /// Creates a replacement map from a JSON string
    /// </summary>
    /// <param name="json">JSON string containing the mappings</param>
    /// <param name="sourceFilePath">Optional source file path</param>
    /// <returns>ReplacementMap instance</returns>
    /// <exception cref="JsonException">Thrown when JSON is invalid</exception>
    /// <exception cref="ArgumentException">Thrown when mappings are invalid</exception>
    public static ReplacementMap FromJson(string json, string? sourceFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON content cannot be empty", nameof(json));

        var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new JsonException("Failed to deserialize JSON to dictionary");

        // Validate and sanitize the mappings
        var sanitizedMappings = new Dictionary<string, string>();
        foreach (var kvp in dictionary)
        {
            if (!IsValidPlaceholderName(kvp.Key))
                throw new ArgumentException($"Invalid placeholder name: {kvp.Key}");

            sanitizedMappings[kvp.Key] = SanitizeReplacementValue(kvp.Value);
        }

        return new ReplacementMap
        {
            Mappings = sanitizedMappings,
            SourceFilePath = sourceFilePath
        };
    }

    /// <summary>
    /// Converts the replacement map to a JSON string
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON representation of the replacement map</returns>
    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(Mappings, options);
    }

    /// <summary>
    /// Merges this replacement map with another, with the other map taking precedence
    /// </summary>
    /// <param name="other">Other replacement map to merge</param>
    /// <returns>New merged replacement map</returns>
    public ReplacementMap MergeWith(ReplacementMap other)
    {
        if (other == null)
            return this;

        var mergedMappings = new Dictionary<string, string>(Mappings);
        
        foreach (var kvp in other.Mappings)
        {
            mergedMappings[kvp.Key] = kvp.Value;
        }

        return new ReplacementMap
        {
            Mappings = mergedMappings,
            SourceFilePath = other.SourceFilePath ?? SourceFilePath
        };
    }
}