namespace DocxTemplate.Core.Services;

public interface ITemplateSetService
{
    /// <summary>
    /// Lists all available template sets in the templates root folder
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of template sets with metadata</returns>
    Task<IEnumerable<TemplateSet>> ListTemplateSetsAsync(
        string templatesRootPath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed information about a specific template set
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="setName">Name of the template set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template set details including all templates</returns>
    Task<TemplateSet> GetTemplateSetAsync(
        string templatesRootPath,
        string setName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a template set exists and contains valid templates
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="setName">Name of the template set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any issues found</returns>
    Task<TemplateSetValidationResult> ValidateTemplateSetAsync(
        string templatesRootPath,
        string setName,
        CancellationToken cancellationToken = default);
}

public record TemplateSetValidationResult
{
    public required bool IsValid { get; init; }
    public required IEnumerable<string> Errors { get; init; }
    public required IEnumerable<string> Warnings { get; init; }
    
    public static TemplateSetValidationResult Success() =>
        new() { IsValid = true, Errors = Array.Empty<string>(), Warnings = Array.Empty<string>() };
        
    public static TemplateSetValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null) =>
        new() { IsValid = false, Errors = errors, Warnings = warnings ?? Array.Empty<string>() };
}

public record TemplateSet
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required IEnumerable<Template> Templates { get; init; }
    public required int TemplateCount { get; init; }
    public required long TotalSizeBytes { get; init; }
    public required DateTime LastModified { get; init; }
    public required bool HasSubfolders { get; init; }
    
    /// <summary>
    /// Gets a display-friendly size string (e.g., "2.3 MB")
    /// </summary>
    public string DisplaySize => FormatBytes(TotalSizeBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
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

public record Template
{
    public required string FileName { get; init; }
    public required string RelativePath { get; init; }  // Relative to template set root
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime LastModified { get; init; }
}