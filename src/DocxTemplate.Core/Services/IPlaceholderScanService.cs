using DocxTemplate.Processing.Models;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;

namespace DocxTemplate.Core.Services;

/// <summary>
/// Service for scanning template documents to discover placeholder patterns
/// </summary>
public interface IPlaceholderScanService
{
    /// <summary>
    /// Scans templates in a folder for placeholder patterns
    /// </summary>
    /// <param name="folderPath">Folder containing templates to scan</param>
    /// <param name="pattern">Regex pattern for placeholders (default: {{.*?}})</param>
    /// <param name="recursive">Include subdirectories in the scan</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Scan results with discovered placeholders and statistics</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the folder path doesn't exist</exception>
    /// <exception cref="InvalidPlaceholderPatternException">Thrown when the pattern is invalid</exception>
    /// <exception cref="FileAccessException">Thrown when files cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<PlaceholderScanResult> ScanPlaceholdersAsync(
        string folderPath,
        string pattern = @"\{\{.*?\}\}",
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans specific template files for placeholder patterns
    /// </summary>
    /// <param name="templateFiles">Collection of template files to scan</param>
    /// <param name="pattern">Regex pattern for placeholders (default: {{.*?}})</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Scan results with discovered placeholders and statistics</returns>
    /// <exception cref="InvalidPlaceholderPatternException">Thrown when the pattern is invalid</exception>
    /// <exception cref="DocumentProcessingException">Thrown when documents cannot be processed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<PlaceholderScanResult> ScanPlaceholdersAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string pattern = @"\{\{.*?\}\}",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a single template file for placeholder patterns
    /// </summary>
    /// <param name="templatePath">Path to the template file to scan</param>
    /// <param name="pattern">Regex pattern for placeholders (default: {{.*?}})</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>List of placeholders found in the file</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the template file doesn't exist</exception>
    /// <exception cref="InvalidPlaceholderPatternException">Thrown when the pattern is invalid</exception>
    /// <exception cref="DocumentProcessingException">Thrown when the document cannot be processed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<IReadOnlyList<Placeholder>> ScanSingleFileAsync(
        string templatePath,
        string pattern = @"\{\{.*?\}\}",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a placeholder pattern is correctly formatted
    /// </summary>
    /// <param name="pattern">Pattern to validate</param>
    /// <returns>True if the pattern is valid</returns>
    /// <exception cref="InvalidPlaceholderPatternException">Thrown when the pattern is invalid</exception>
    bool ValidatePattern(string pattern);

    /// <summary>
    /// Extracts placeholder names from raw text content
    /// </summary>
    /// <param name="content">Text content to scan</param>
    /// <param name="pattern">Regex pattern for placeholders</param>
    /// <returns>Collection of unique placeholder names found</returns>
    /// <exception cref="InvalidPlaceholderPatternException">Thrown when the pattern is invalid</exception>
    IReadOnlyList<string> ExtractPlaceholderNames(string content, string pattern = @"\{\{.*?\}\}");

    /// <summary>
    /// Gets statistics about placeholder usage across multiple files
    /// </summary>
    /// <param name="scanResult">Results from a previous scan operation</param>
    /// <returns>Statistics about placeholder distribution and usage</returns>
    /// <exception cref="ArgumentNullException">Thrown when scan result is null</exception>
    PlaceholderStatistics GetPlaceholderStatistics(PlaceholderScanResult scanResult);
}