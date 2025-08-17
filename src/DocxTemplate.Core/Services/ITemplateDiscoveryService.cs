using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;

namespace DocxTemplate.Core.Services;

/// <summary>
/// Service for discovering .docx template files in directories
/// </summary>
public interface ITemplateDiscoveryService
{
    /// <summary>
    /// Discovers all .docx template files in the specified directory
    /// </summary>
    /// <param name="folderPath">Path to search for templates</param>
    /// <param name="recursive">Include subdirectories in the search</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Collection of discovered template files</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the folder path doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when the folder cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when folder path is invalid</exception>
    Task<IReadOnlyList<TemplateFile>> DiscoverTemplatesAsync(
        string folderPath, 
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers templates matching specific patterns
    /// </summary>
    /// <param name="folderPath">Path to search for templates</param>
    /// <param name="filePatterns">File patterns to match (e.g., "*.docx", "template*.docx")</param>
    /// <param name="recursive">Include subdirectories in the search</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Collection of discovered template files matching the patterns</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the folder path doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when the folder cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when folder path or patterns are invalid</exception>
    Task<IReadOnlyList<TemplateFile>> DiscoverTemplatesAsync(
        string folderPath,
        IReadOnlyList<string> filePatterns,
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a template file exists and is accessible
    /// </summary>
    /// <param name="templatePath">Path to the template file to validate</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>True if the template is valid and accessible</returns>
    /// <exception cref="ArgumentException">Thrown when template path is invalid</exception>
    Task<bool> ValidateTemplateAsync(
        string templatePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific template file
    /// </summary>
    /// <param name="templatePath">Path to the template file</param>
    /// <param name="basePath">Base path for calculating relative path</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Template file metadata</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the template file doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when the template file cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
    Task<TemplateFile> GetTemplateMetadataAsync(
        string templatePath,
        string? basePath = null,
        CancellationToken cancellationToken = default);
}