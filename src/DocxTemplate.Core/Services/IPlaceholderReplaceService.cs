using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;

namespace DocxTemplate.Core.Services;

/// <summary>
/// Service for replacing placeholders in template documents with actual values
/// </summary>
public interface IPlaceholderReplaceService
{
    /// <summary>
    /// Replaces placeholders in templates with provided values
    /// </summary>
    /// <param name="folderPath">Folder containing templates to process</param>
    /// <param name="replacementMap">Map of placeholder names to replacement values</param>
    /// <param name="createBackup">Whether to create backup copies before replacing</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Replacement operation result with statistics and file details</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the folder path doesn't exist</exception>
    /// <exception cref="ReplacementValidationException">Thrown when replacement mappings are invalid</exception>
    /// <exception cref="FileAccessException">Thrown when files cannot be accessed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<ReplaceResult> ReplacePlaceholdersAsync(
        string folderPath,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces placeholders in specific template files
    /// </summary>
    /// <param name="templateFiles">Collection of template files to process</param>
    /// <param name="replacementMap">Map of placeholder names to replacement values</param>
    /// <param name="createBackup">Whether to create backup copies before replacing</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Replacement operation result with statistics and file details</returns>
    /// <exception cref="ReplacementValidationException">Thrown when replacement mappings are invalid</exception>
    /// <exception cref="DocumentProcessingException">Thrown when documents cannot be processed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<ReplaceResult> ReplacePlaceholdersAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces placeholders in a single template file
    /// </summary>
    /// <param name="templatePath">Path to the template file to process</param>
    /// <param name="replacementMap">Map of placeholder names to replacement values</param>
    /// <param name="createBackup">Whether to create a backup copy before replacing</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>File replacement result</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the template file doesn't exist</exception>
    /// <exception cref="ReplacementValidationException">Thrown when replacement mappings are invalid</exception>
    /// <exception cref="DocumentProcessingException">Thrown when the document cannot be processed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<FileReplaceResult> ReplacePlaceholdersInFileAsync(
        string templatePath,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews what replacements would be made without actually modifying files
    /// </summary>
    /// <param name="folderPath">Folder containing templates to preview</param>
    /// <param name="replacementMap">Map of placeholder names to replacement values</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Preview results showing what would be replaced</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when the folder path doesn't exist</exception>
    /// <exception cref="ReplacementValidationException">Thrown when replacement mappings are invalid</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<ReplacementPreview> PreviewReplacementsAsync(
        string folderPath,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates replacement mappings against discovered placeholders
    /// </summary>
    /// <param name="placeholders">Placeholders found in templates</param>
    /// <param name="replacementMap">Replacement mappings to validate</param>
    /// <param name="requireAllPlaceholders">Whether all placeholders must have replacements</param>
    /// <returns>Validation result with any issues found</returns>
    /// <exception cref="ArgumentNullException">Thrown when arguments are null</exception>
    ReplacementValidationResult ValidateReplacements(
        IReadOnlyList<Placeholder> placeholders,
        ReplacementMap replacementMap,
        bool requireAllPlaceholders = false);

    /// <summary>
    /// Replaces placeholders in a single template file
    /// </summary>
    /// <param name="templateFile">Template file to process</param>
    /// <param name="replacementMap">Map of placeholder names to replacement values</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Replacement operation result</returns>
    /// <exception cref="DocumentProcessingException">Thrown when the document cannot be processed</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<ReplaceResult> ReplaceInTemplateAsync(
        TemplateFile templateFile,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates backup copies of template files
    /// </summary>
    /// <param name="templateFiles">Files to backup</param>
    /// <param name="backupDirectory">Directory to store backups (optional)</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Backup operation result</returns>
    /// <exception cref="FileAccessException">Thrown when backup operations fail</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<BackupResult> CreateBackupsAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string? backupDirectory = null,
        CancellationToken cancellationToken = default);
}