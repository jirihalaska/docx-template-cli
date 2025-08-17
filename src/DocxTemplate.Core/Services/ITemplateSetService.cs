using DocxTemplate.Core.Models;
using DocxTemplate.Core.Exceptions;

namespace DocxTemplate.Core.Services;

/// <summary>
/// Service for managing collections of templates organized as sets
/// </summary>
public interface ITemplateSetService
{
    /// <summary>
    /// Lists all available template sets in the templates root folder
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="includeEmptyFolders">Whether to include folders without .docx files</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Collection of template sets with metadata</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when templates root path doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when templates root folder cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when path is invalid</exception>
    Task<IReadOnlyList<TemplateSet>> ListTemplateSetsAsync(
        string templatesRootPath,
        bool includeEmptyFolders = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed information about a specific template set
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="setName">Name of the template set (directory name)</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Template set details including all templates</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when template set doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when template set cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<TemplateSet> GetTemplateSetAsync(
        string templatesRootPath,
        string setName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets template set information from a direct path
    /// </summary>
    /// <param name="templateSetPath">Direct path to the template set directory</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Template set details</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when path doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when path cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when path is invalid</exception>
    Task<TemplateSet> GetTemplateSetFromPathAsync(
        string templateSetPath,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a template set exists and contains valid templates
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="setName">Name of the template set</param>
    /// <param name="requireMinimumFiles">Minimum number of template files required</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Validation result with any issues found</returns>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<TemplateSetValidationResult> ValidateTemplateSetAsync(
        string templatesRootPath,
        string setName,
        int requireMinimumFiles = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates template sets in bulk
    /// </summary>
    /// <param name="templateSets">Template sets to validate</param>
    /// <param name="requireMinimumFiles">Minimum number of template files required per set</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Dictionary of validation results by template set name</returns>
    /// <exception cref="ArgumentNullException">Thrown when template sets collection is null</exception>
    Task<IReadOnlyDictionary<string, TemplateSetValidationResult>> ValidateTemplateSetsAsync(
        IReadOnlyList<TemplateSet> templateSets,
        int requireMinimumFiles = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for template sets containing specific file patterns
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="filePatterns">File patterns to search for</param>
    /// <param name="requireAllPatterns">Whether all patterns must be present in a set</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Template sets matching the search criteria</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when templates root path doesn't exist</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<IReadOnlyList<TemplateSet>> SearchTemplateSetsAsync(
        string templatesRootPath,
        IReadOnlyList<string> filePatterns,
        bool requireAllPatterns = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about all template sets in the root folder
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Statistical summary of all template sets</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when templates root path doesn't exist</exception>
    /// <exception cref="ArgumentException">Thrown when path is invalid</exception>
    Task<TemplateSetStatistics> GetTemplateSetStatisticsAsync(
        string templatesRootPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new template set by copying from an existing template set
    /// </summary>
    /// <param name="sourceSetPath">Path to source template set</param>
    /// <param name="targetSetPath">Path where new template set will be created</param>
    /// <param name="preserveStructure">Whether to preserve directory structure</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Copy result with details of the operation</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when source set doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when copy operation fails</exception>
    /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
    Task<Models.Results.CopyResult> CreateTemplateSetCopyAsync(
        string sourceSetPath,
        string targetSetPath,
        bool preserveStructure = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a template set name is valid for the file system
    /// </summary>
    /// <param name="setName">Template set name to validate</param>
    /// <returns>True if the name is valid</returns>
    bool IsValidTemplateSetName(string setName);

    /// <summary>
    /// Gets suggested names for a template set based on existing sets
    /// </summary>
    /// <param name="templatesRootPath">Path to templates root folder</param>
    /// <param name="baseName">Base name for suggestions</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>List of suggested template set names</returns>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<IReadOnlyList<string>> GetSuggestedTemplateSetNamesAsync(
        string templatesRootPath,
        string baseName,
        int maxSuggestions = 5,
        CancellationToken cancellationToken = default);
}