using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;

namespace DocxTemplate.Core.Services;

/// <summary>
/// Service for copying template files from source to target locations
/// </summary>
public interface ITemplateCopyService
{
    /// <summary>
    /// Copies templates from source to target directory
    /// </summary>
    /// <param name="sourcePath">Source directory path containing templates</param>
    /// <param name="targetPath">Target directory path where templates will be copied</param>
    /// <param name="preserveStructure">Whether to maintain directory structure in target</param>
    /// <param name="overwrite">Whether to overwrite existing files in target</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Copy operation result with statistics and file details</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when source path doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when files cannot be accessed or copied</exception>
    /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
    Task<CopyResult> CopyTemplatesAsync(
        string sourcePath,
        string targetPath,
        bool preserveStructure = true,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies specific template files to target directory
    /// </summary>
    /// <param name="templateFiles">Collection of template files to copy</param>
    /// <param name="targetPath">Target directory path where files will be copied</param>
    /// <param name="preserveStructure">Whether to maintain directory structure in target</param>
    /// <param name="overwrite">Whether to overwrite existing files in target</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Copy operation result with statistics and file details</returns>
    /// <exception cref="FileAccessException">Thrown when files cannot be accessed or copied</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<CopyResult> CopyTemplatesAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        bool preserveStructure = true,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a single template file to target location
    /// </summary>
    /// <param name="sourceFile">Template file to copy</param>
    /// <param name="targetPath">Target file path or directory</param>
    /// <param name="overwrite">Whether to overwrite if target exists</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Details of the copied file</returns>
    /// <exception cref="TemplateNotFoundException">Thrown when source file doesn't exist</exception>
    /// <exception cref="FileAccessException">Thrown when file cannot be copied</exception>
    /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
    Task<CopiedFile> CopyTemplateFileAsync(
        TemplateFile sourceFile,
        string targetPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that templates can be copied to the target location
    /// </summary>
    /// <param name="sourcePath">Source directory path</param>
    /// <param name="targetPath">Target directory path</param>
    /// <param name="overwrite">Whether overwriting is allowed</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Validation result with any issues found</returns>
    /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
    Task<CopyValidationResult> ValidateCopyOperationAsync(
        string sourcePath,
        string targetPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates disk space required for copy operation
    /// </summary>
    /// <param name="templateFiles">Template files to copy</param>
    /// <param name="targetPath">Target directory path</param>
    /// <param name="preserveStructure">Whether directory structure will be preserved</param>
    /// <returns>Estimated disk space requirements</returns>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
    Task<CopySpaceEstimate> EstimateCopySpaceAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        bool preserveStructure = true);

    /// <summary>
    /// Creates target directory structure without copying files
    /// </summary>
    /// <param name="templateFiles">Template files that define the structure</param>
    /// <param name="targetPath">Target directory path</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>List of directories that were created</returns>
    /// <exception cref="FileAccessException">Thrown when directories cannot be created</exception>
    /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
    Task<IReadOnlyList<string>> CreateDirectoryStructureAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        string? sourcePath = null,
        CancellationToken cancellationToken = default);
}