namespace DocxTemplate.Core.Exceptions;

/// <summary>
/// Exception thrown when replacement mappings are invalid or cannot be applied
/// </summary>
public class ReplacementValidationException : DocxTemplateException
{
    /// <summary>
    /// The placeholder name that caused the validation error
    /// </summary>
    public string? PlaceholderName { get; }

    /// <summary>
    /// The replacement value that failed validation
    /// </summary>
    public string? ReplacementValue { get; }

    /// <summary>
    /// The validation rule that was violated
    /// </summary>
    public string? ValidationRule { get; }

    /// <summary>
    /// Collection of validation errors for multiple placeholders
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the ReplacementValidationException class
    /// </summary>
    public ReplacementValidationException() : base("Replacement validation failed")
    {
        ValidationErrors = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the ReplacementValidationException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ReplacementValidationException(string message) : base(message)
    {
        ValidationErrors = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the ReplacementValidationException class for a specific placeholder
    /// </summary>
    /// <param name="placeholderName">Name of the placeholder that failed validation</param>
    /// <param name="validationRule">The validation rule that was violated</param>
    public ReplacementValidationException(string placeholderName, string validationRule) 
        : base($"Validation failed for placeholder '{placeholderName}': {validationRule}")
    {
        PlaceholderName = placeholderName;
        ValidationRule = validationRule;
        ValidationErrors = new[] { $"{placeholderName}: {validationRule}" };
    }

    /// <summary>
    /// Initializes a new instance of the ReplacementValidationException class for a specific placeholder and value
    /// </summary>
    /// <param name="placeholderName">Name of the placeholder that failed validation</param>
    /// <param name="replacementValue">The replacement value that failed validation</param>
    /// <param name="validationRule">The validation rule that was violated</param>
    public ReplacementValidationException(string placeholderName, string replacementValue, string validationRule) 
        : base($"Validation failed for placeholder '{placeholderName}' with value '{replacementValue}': {validationRule}")
    {
        PlaceholderName = placeholderName;
        ReplacementValue = replacementValue;
        ValidationRule = validationRule;
        ValidationErrors = new[] { $"{placeholderName}: {validationRule}" };
    }

    /// <summary>
    /// Initializes a new instance of the ReplacementValidationException class with multiple validation errors
    /// </summary>
    /// <param name="validationErrors">Collection of validation error messages</param>
    public ReplacementValidationException(IReadOnlyList<string> validationErrors) 
        : base($"Replacement validation failed with {validationErrors.Count} error(s): {string.Join("; ", validationErrors.Take(3))}{(validationErrors.Count > 3 ? "..." : "")}")
    {
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the ReplacementValidationException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public ReplacementValidationException(string message, Exception innerException) : base(message, innerException)
    {
        ValidationErrors = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a ReplacementValidationException for an empty placeholder name
    /// </summary>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException EmptyPlaceholderName()
    {
        return new ReplacementValidationException("", "Placeholder name cannot be empty or whitespace");
    }

    /// <summary>
    /// Creates a ReplacementValidationException for a placeholder name that's too long
    /// </summary>
    /// <param name="placeholderName">The placeholder name that's too long</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException PlaceholderNameTooLong(string placeholderName, int maxLength)
    {
        return new ReplacementValidationException(placeholderName, $"Placeholder name length ({placeholderName.Length}) exceeds maximum allowed length ({maxLength})");
    }

    /// <summary>
    /// Creates a ReplacementValidationException for invalid characters in placeholder name
    /// </summary>
    /// <param name="placeholderName">The placeholder name with invalid characters</param>
    /// <param name="invalidChars">Description of invalid characters</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException InvalidPlaceholderCharacters(string placeholderName, string invalidChars)
    {
        return new ReplacementValidationException(placeholderName, $"Placeholder name contains invalid characters: {invalidChars}");
    }

    /// <summary>
    /// Creates a ReplacementValidationException for a replacement value that's too long
    /// </summary>
    /// <param name="placeholderName">Name of the placeholder</param>
    /// <param name="replacementValue">The replacement value that's too long</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException ReplacementValueTooLong(string placeholderName, string replacementValue, int maxLength)
    {
        var truncatedValue = replacementValue.Length > 50 ? replacementValue.Substring(0, 47) + "..." : replacementValue;
        return new ReplacementValidationException(placeholderName, truncatedValue, $"Replacement value length ({replacementValue.Length}) exceeds maximum allowed length ({maxLength})");
    }

    /// <summary>
    /// Creates a ReplacementValidationException for invalid characters in replacement value
    /// </summary>
    /// <param name="placeholderName">Name of the placeholder</param>
    /// <param name="replacementValue">The replacement value with invalid characters</param>
    /// <param name="invalidChars">Description of invalid characters</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException InvalidReplacementCharacters(string placeholderName, string replacementValue, string invalidChars)
    {
        var truncatedValue = replacementValue.Length > 50 ? replacementValue.Substring(0, 47) + "..." : replacementValue;
        return new ReplacementValidationException(placeholderName, truncatedValue, $"Replacement value contains invalid characters: {invalidChars}");
    }

    /// <summary>
    /// Creates a ReplacementValidationException for duplicate placeholder names
    /// </summary>
    /// <param name="placeholderName">The duplicate placeholder name</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException DuplicatePlaceholder(string placeholderName)
    {
        return new ReplacementValidationException(placeholderName, "Duplicate placeholder name found (case-insensitive comparison)");
    }

    /// <summary>
    /// Creates a ReplacementValidationException for required placeholders that are missing
    /// </summary>
    /// <param name="missingPlaceholders">List of missing placeholder names</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException MissingRequiredPlaceholders(IReadOnlyList<string> missingPlaceholders)
    {
        var errors = missingPlaceholders.Select(p => $"{p}: Required placeholder is missing from replacement map").ToList();
        return new ReplacementValidationException(errors);
    }

    /// <summary>
    /// Creates a ReplacementValidationException for JSON deserialization errors
    /// </summary>
    /// <param name="jsonError">Description of the JSON error</param>
    /// <param name="innerException">The JSON exception</param>
    /// <returns>ReplacementValidationException instance</returns>
    public static ReplacementValidationException JsonDeserializationFailed(string jsonError, Exception innerException)
    {
        return new ReplacementValidationException($"Failed to deserialize replacement mappings from JSON: {jsonError}", innerException);
    }

    /// <summary>
    /// Gets a summary of all validation errors
    /// </summary>
    /// <returns>Formatted string with all validation errors</returns>
    public string GetValidationSummary()
    {
        if (ValidationErrors.Count == 0)
            return Message;

        if (ValidationErrors.Count == 1)
            return ValidationErrors[0];

        return $"Multiple validation errors ({ValidationErrors.Count}):\n" + 
               string.Join("\n", ValidationErrors.Select((error, index) => $"  {index + 1}. {error}"));
    }
}