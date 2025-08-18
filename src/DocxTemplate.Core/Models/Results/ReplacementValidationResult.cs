using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Models.Results;

/// <summary>
/// Result of validating replacement mappings against discovered placeholders
/// </summary>
public record ReplacementValidationResult
{
    /// <summary>
    /// Indicates whether the replacement mappings are valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors found
    /// </summary>
    [Required(ErrorMessage = "Errors collection is required")]
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// List of validation warnings found
    /// </summary>
    [Required(ErrorMessage = "Warnings collection is required")]
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Placeholders that have valid replacement mappings
    /// </summary>
    [Required(ErrorMessage = "Valid mappings collection is required")]
    public required IReadOnlyList<string> ValidMappings { get; init; }

    /// <summary>
    /// Required placeholders that are missing mappings
    /// </summary>
    [Required(ErrorMessage = "Missing required mappings collection is required")]
    public required IReadOnlyList<string> MissingRequiredMappings { get; init; }

    /// <summary>
    /// Optional placeholders that don't have mappings
    /// </summary>
    [Required(ErrorMessage = "Missing optional mappings collection is required")]
    public required IReadOnlyList<string> MissingOptionalMappings { get; init; }

    /// <summary>
    /// Replacement mappings that don't correspond to any discovered placeholders
    /// </summary>
    [Required(ErrorMessage = "Unused mappings collection is required")]
    public required IReadOnlyList<string> UnusedMappings { get; init; }

    /// <summary>
    /// Mappings with invalid values
    /// </summary>
    [Required(ErrorMessage = "Invalid mappings collection is required")]
    public required IReadOnlyList<InvalidMapping> InvalidMappings { get; init; }

    /// <summary>
    /// Timestamp when the validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the validation process
    /// </summary>
    public TimeSpan ValidationDuration { get; init; }

    /// <summary>
    /// Number of placeholders that were validated
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Placeholders validated must be non-negative")]
    public int PlaceholdersValidated { get; init; }

    /// <summary>
    /// Number of replacement mappings that were validated
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Mappings validated must be non-negative")]
    public int MappingsValidated { get; init; }

    /// <summary>
    /// Indicates whether there are any issues (errors or warnings)
    /// </summary>
    public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;

    /// <summary>
    /// Percentage of placeholders that have valid mappings
    /// </summary>
    public double MappingCoveragePercentage
    {
        get
        {
            var totalRequired = ValidMappings.Count + MissingRequiredMappings.Count + MissingOptionalMappings.Count;
            return totalRequired > 0 ? (ValidMappings.Count * 100.0) / totalRequired : 100.0;
        }
    }

    /// <summary>
    /// Percentage of required placeholders that have mappings
    /// </summary>
    public double RequiredMappingCoveragePercentage
    {
        get
        {
            var totalRequired = ValidMappings.Count + MissingRequiredMappings.Count;
            return totalRequired > 0 ? (ValidMappings.Count * 100.0) / totalRequired : 100.0;
        }
    }

    /// <summary>
    /// Gets a summary of the validation result
    /// </summary>
    public string GetSummary()
    {
        var summary = $"Validation: {(IsValid ? "Valid" : "Invalid")}. ";
        summary += $"Coverage: {MappingCoveragePercentage:F1}% ({ValidMappings.Count}/{PlaceholdersValidated} placeholders). ";
        
        if (Errors.Count > 0)
            summary += $"{Errors.Count} error{(Errors.Count == 1 ? "" : "s")}. ";
            
        if (Warnings.Count > 0)
            summary += $"{Warnings.Count} warning{(Warnings.Count == 1 ? "" : "s")}. ";

        if (MissingRequiredMappings.Count > 0)
            summary += $"{MissingRequiredMappings.Count} missing required mapping{(MissingRequiredMappings.Count == 1 ? "" : "s")}. ";

        if (UnusedMappings.Count > 0)
            summary += $"{UnusedMappings.Count} unused mapping{(UnusedMappings.Count == 1 ? "" : "s")}. ";

        summary += $"Validated in {ValidationDuration.TotalMilliseconds:F0}ms.";

        return summary;
    }

    /// <summary>
    /// Gets all issues (errors and warnings) as a formatted string
    /// </summary>
    public string GetAllIssues()
    {
        var issues = new List<string>();
        
        foreach (var error in Errors)
            issues.Add($"ERROR: {error}");
            
        foreach (var warning in Warnings)
            issues.Add($"WARNING: {warning}");

        return string.Join("\n", issues);
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <param name="validMappings">Placeholders with valid mappings</param>
    /// <param name="missingOptionalMappings">Optional placeholders without mappings</param>
    /// <param name="unusedMappings">Unused replacement mappings</param>
    /// <param name="validationDuration">Duration of validation</param>
    /// <param name="warnings">Optional warnings</param>
    /// <returns>ReplacementValidationResult instance</returns>
    public static ReplacementValidationResult Success(
        IReadOnlyList<string> validMappings,
        IReadOnlyList<string>? missingOptionalMappings = null,
        IReadOnlyList<string>? unusedMappings = null,
        TimeSpan validationDuration = default,
        IReadOnlyList<string>? warnings = null)
    {
        return new ReplacementValidationResult
        {
            IsValid = true,
            Errors = [],
            Warnings = warnings ?? [],
            ValidMappings = validMappings,
            MissingRequiredMappings = [],
            MissingOptionalMappings = missingOptionalMappings ?? [],
            UnusedMappings = unusedMappings ?? [],
            InvalidMappings = [],
            ValidationDuration = validationDuration,
            PlaceholdersValidated = validMappings.Count + (missingOptionalMappings?.Count ?? 0),
            MappingsValidated = validMappings.Count + (unusedMappings?.Count ?? 0)
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="validMappings">Valid mappings</param>
    /// <param name="missingRequiredMappings">Missing required mappings</param>
    /// <param name="missingOptionalMappings">Missing optional mappings</param>
    /// <param name="unusedMappings">Unused mappings</param>
    /// <param name="invalidMappings">Invalid mappings</param>
    /// <param name="validationDuration">Duration of validation</param>
    /// <param name="warnings">Optional warnings</param>
    /// <returns>ReplacementValidationResult instance</returns>
    public static ReplacementValidationResult Failure(
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? validMappings = null,
        IReadOnlyList<string>? missingRequiredMappings = null,
        IReadOnlyList<string>? missingOptionalMappings = null,
        IReadOnlyList<string>? unusedMappings = null,
        IReadOnlyList<InvalidMapping>? invalidMappings = null,
        TimeSpan validationDuration = default,
        IReadOnlyList<string>? warnings = null)
    {
        var validCount = validMappings?.Count ?? 0;
        var missingRequired = missingRequiredMappings?.Count ?? 0;
        var missingOptional = missingOptionalMappings?.Count ?? 0;
        var unusedCount = unusedMappings?.Count ?? 0;

        return new ReplacementValidationResult
        {
            IsValid = false,
            Errors = errors,
            Warnings = warnings ?? [],
            ValidMappings = validMappings ?? [],
            MissingRequiredMappings = missingRequiredMappings ?? [],
            MissingOptionalMappings = missingOptionalMappings ?? [],
            UnusedMappings = unusedMappings ?? [],
            InvalidMappings = invalidMappings ?? [],
            ValidationDuration = validationDuration,
            PlaceholdersValidated = validCount + missingRequired + missingOptional,
            MappingsValidated = validCount + unusedCount
        };
    }
}

/// <summary>
/// Represents an invalid replacement mapping
/// </summary>
public record InvalidMapping
{
    /// <summary>
    /// Name of the placeholder with invalid mapping
    /// </summary>
    [Required(ErrorMessage = "Placeholder name is required")]
    public required string PlaceholderName { get; init; }

    /// <summary>
    /// The invalid replacement value
    /// </summary>
    public string? ReplacementValue { get; init; }

    /// <summary>
    /// Reason why the mapping is invalid
    /// </summary>
    [Required(ErrorMessage = "Validation error is required")]
    public required string ValidationError { get; init; }

    /// <summary>
    /// Severity of the validation issue
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Gets a display string for this invalid mapping
    /// </summary>
    public string DisplayError => $"{PlaceholderName}: {ValidationError}";
}

/// <summary>
/// Severity levels for validation issues
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Information only
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be reviewed
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents successful processing
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that indicates serious issues
    /// </summary>
    Critical
}