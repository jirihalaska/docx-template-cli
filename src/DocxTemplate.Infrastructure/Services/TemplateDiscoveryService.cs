using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for discovering .docx template files in directories
/// </summary>
public class TemplateDiscoveryService : ITemplateDiscoveryService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<TemplateDiscoveryService> _logger;

    public TemplateDiscoveryService(
        IFileSystemService fileSystemService,
        ILogger<TemplateDiscoveryService> logger)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateFile>> DiscoverTemplatesAsync(
        string folderPath, 
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        return await DiscoverTemplatesAsync(
            folderPath, 
            new[] { "*.docx" }, 
            recursive, 
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateFile>> DiscoverTemplatesAsync(
        string folderPath,
        IReadOnlyList<string> filePatterns,
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));

        if (filePatterns == null || filePatterns.Count == 0)
            throw new ArgumentException("File patterns cannot be null or empty.", nameof(filePatterns));

        if (!_fileSystemService.DirectoryExists(folderPath))
            throw new TemplateNotFoundException($"Folder path not found: {folderPath}");

        try
        {
            var templates = new List<TemplateFile>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var pattern in filePatterns)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var files = _fileSystemService.EnumerateFiles(folderPath, pattern, searchOption);
                    
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Skip temporary files
                        var fileName = Path.GetFileName(file);
                        if (fileName.StartsWith("~$"))
                        {
                            _logger.LogDebug("Skipping temporary file: {File}", file);
                            continue;
                        }

                        // Skip hidden files on Unix systems
                        if (fileName.StartsWith('.'))
                        {
                            _logger.LogDebug("Skipping hidden file: {File}", file);
                            continue;
                        }

                        try
                        {
                            var templateFile = await GetTemplateMetadataAsync(file, folderPath, cancellationToken);
                            templates.Add(templateFile);
                            _logger.LogDebug("Discovered template: {File}", file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process file: {File}", file);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied to directory in: {Path}", folderPath);
                }
                catch (DirectoryNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Directory not found: {Path}", folderPath);
                }
            }

            _logger.LogInformation("Discovered {Count} templates in {Path}", templates.Count, folderPath);
            return templates.AsReadOnly();
        }
        catch (Exception ex) when (ex is not TemplateNotFoundException && ex is not ArgumentException)
        {
            throw new FileAccessException($"Error accessing folder: {folderPath}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateTemplateAsync(
        string templatePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if file exists
            if (!_fileSystemService.FileExists(templatePath))
                return false;

            // Check if it has .docx extension
            var extension = Path.GetExtension(templatePath);
            if (!extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check if it's not a temporary file
            var fileName = Path.GetFileName(templatePath);
            if (fileName.StartsWith("~$"))
                return false;

            // Try to open the file to verify it's accessible
            try
            {
                // Just check if we can get file size - this is a quick access check
                var size = _fileSystemService.GetFileSize(templatePath);
                return size > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot access template file: {Path}", templatePath);
                return false;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TemplateFile> GetTemplateMetadataAsync(
        string templatePath,
        string? basePath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));

        if (!_fileSystemService.FileExists(templatePath))
            throw new TemplateNotFoundException($"Template file not found: {templatePath}");

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fullPath = _fileSystemService.GetFullPath(templatePath);
                var fileName = _fileSystemService.GetFileName(fullPath);
                var fileSize = _fileSystemService.GetFileSize(fullPath);
                var lastModified = _fileSystemService.GetLastWriteTime(fullPath);

                // Calculate relative path
                string relativePath;
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    var baseFullPath = _fileSystemService.GetFullPath(basePath);
                    relativePath = Path.GetRelativePath(baseFullPath, fullPath);
                }
                else
                {
                    relativePath = fileName;
                }

                return new TemplateFile
                {
                    FullPath = fullPath,
                    RelativePath = relativePath,
                    FileName = fileName,
                    SizeInBytes = fileSize,
                    LastModified = lastModified.ToUniversalTime()
                };
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied to template file: {templatePath}", ex);
        }
        catch (Exception ex) when (ex is not TemplateNotFoundException && ex is not FileAccessException)
        {
            throw new FileAccessException($"Error accessing template file: {templatePath}", ex);
        }
    }
}