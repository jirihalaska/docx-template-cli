using System.Collections.Concurrent;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for copying template files from source to target locations with comprehensive features
/// </summary>
public class TemplateCopyService : ITemplateCopyService
{
    private readonly ITemplateDiscoveryService _discoveryService;
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<TemplateCopyService> _logger;

    public TemplateCopyService(
        ITemplateDiscoveryService discoveryService,
        IFileSystemService fileSystemService,
        ILogger<TemplateCopyService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CopyResult> CopyTemplatesAsync(
        string sourcePath,
        string targetPath,
        bool preserveStructure = true,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be null or empty.", nameof(sourcePath));

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        if (!_fileSystemService.DirectoryExists(sourcePath))
            throw new TemplateNotFoundException($"Source directory not found: {sourcePath}");

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting copy operation from {SourcePath} to {TargetPath}", sourcePath, targetPath);

        try
        {
            var templateFiles = await _discoveryService.DiscoverTemplatesAsync(sourcePath, recursive: true, cancellationToken);
            return await CopyTemplatesAsync(templateFiles, targetPath, preserveStructure, overwrite, sourcePath, cancellationToken);
        }
        catch (Exception ex) when (ex is not TemplateNotFoundException && ex is not FileAccessException)
        {
            _logger.LogError(ex, "Failed to copy templates from {SourcePath} to {TargetPath}", sourcePath, targetPath);
            var duration = DateTime.UtcNow - startTime;
            
            return CopyResult.WithFailures(
                [],
                duration,
                0,
                [new CopyError { SourcePath = sourcePath, TargetPath = targetPath, Message = ex.Message, ExceptionType = ex.GetType().Name }
                ]);
        }
    }

    /// <inheritdoc />
    public async Task<CopyResult> CopyTemplatesAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        bool preserveStructure = true,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        return await CopyTemplatesAsync(templateFiles, targetPath, preserveStructure, overwrite, null, cancellationToken);
    }

    /// <summary>
    /// Internal overload that includes source path for maintaining directory structure
    /// </summary>
    private async Task<CopyResult> CopyTemplatesAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        bool preserveStructure,
        bool overwrite,
        string? sourcePath,
        CancellationToken cancellationToken)
    {
        if (templateFiles == null)
            throw new ArgumentNullException(nameof(templateFiles));

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        var startTime = DateTime.UtcNow;
        var copiedFiles = new ConcurrentBag<CopiedFile>();
        var errors = new ConcurrentBag<CopyError>();
        int failedFiles = 0;

        _logger.LogInformation("Copying {Count} template files to {TargetPath}", templateFiles.Count, targetPath);

        // Ensure target directory exists
        try
        {
            if (!_fileSystemService.DirectoryExists(targetPath))
            {
                _fileSystemService.CreateDirectory(targetPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create target directory: {TargetPath}", targetPath);
            var failureDuration = DateTime.UtcNow - startTime;
            
            return CopyResult.WithFailures(
                [],
                failureDuration,
                templateFiles.Count,
                [new CopyError { SourcePath = string.Empty, TargetPath = targetPath, Message = $"Failed to create target directory: {ex.Message}", ExceptionType = ex.GetType().Name }
                ]);
        }

        // Create directory structure if preserving structure
        if (preserveStructure)
        {
            try
            {
                await CreateDirectoryStructureAsync(templateFiles, targetPath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create directory structure for {TargetPath}", targetPath);
                // Continue with copying even if directory structure creation partially fails
            }
        }

        // Use parallel processing for better performance
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var tasks = templateFiles.Select(async (templateFile) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var copiedFile = await CopyTemplateFileAsync(templateFile, targetPath, overwrite, preserveStructure, sourcePath, cancellationToken);
                copiedFiles.Add(copiedFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy file: {SourcePath}", templateFile.FullPath);
                Interlocked.Increment(ref failedFiles);
                errors.Add(new CopyError
                {
                    SourcePath = templateFile.FullPath,
                    TargetPath = GetTargetFilePath(templateFile, targetPath, preserveStructure, sourcePath),
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name
                });
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var duration = DateTime.UtcNow - startTime;
        var finalCopiedFiles = copiedFiles.ToList();
        var finalErrors = errors.ToList();

        _logger.LogInformation("Completed copy operation. Copied {SuccessCount} files, failed {FailedCount} files in {Duration}ms",
            finalCopiedFiles.Count, failedFiles, duration.TotalMilliseconds);

        return finalErrors.Count > 0 || failedFiles > 0
            ? CopyResult.WithFailures(finalCopiedFiles, duration, failedFiles, finalErrors)
            : CopyResult.Success(finalCopiedFiles, duration);
    }

    /// <inheritdoc />
    public Task<CopiedFile> CopyTemplateFileAsync(
        TemplateFile sourceFile,
        string targetPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        return CopyTemplateFileAsync(sourceFile, targetPath, overwrite, preserveStructure: true, sourcePath: null, cancellationToken);
    }

    /// <summary>
    /// Internal overload that includes structure and source path parameters
    /// </summary>
    private Task<CopiedFile> CopyTemplateFileAsync(
        TemplateFile sourceFile,
        string targetPath,
        bool overwrite,
        bool preserveStructure,
        string? sourcePath,
        CancellationToken cancellationToken)
    {
        if (sourceFile == null)
            throw new ArgumentNullException(nameof(sourceFile));

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        if (!_fileSystemService.FileExists(sourceFile.FullPath))
            throw new TemplateNotFoundException($"Source file not found: {sourceFile.FullPath}");

        var startTime = DateTime.UtcNow;

        // Determine the target file path
        string targetFilePath;
        if (_fileSystemService.DirectoryExists(targetPath))
        {
            // targetPath is a directory, calculate path based on structure preservation
            targetFilePath = GetTargetFilePath(sourceFile, targetPath, preserveStructure, sourcePath);
            var targetDir = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDir) && !_fileSystemService.DirectoryExists(targetDir))
            {
                _fileSystemService.CreateDirectory(targetDir);
            }
        }
        else
        {
            // targetPath is a specific file path
            targetFilePath = targetPath;
            var targetDir = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDir) && !_fileSystemService.DirectoryExists(targetDir))
            {
                _fileSystemService.CreateDirectory(targetDir);
            }
        }

        // Check for existing file and handle overwrite logic
        if (_fileSystemService.FileExists(targetFilePath))
        {
            if (!overwrite)
            {
                throw new FileAccessException($"Target file already exists and overwrite is disabled: {targetFilePath}");
            }

            // Check if target file is read-only
            var fileInfo = new FileInfo(targetFilePath);
            if (fileInfo.IsReadOnly)
            {
                throw new FileAccessException($"Target file is read-only and cannot be overwritten: {targetFilePath}");
            }
        }

        try
        {
            // Perform the actual file copy using the file system service
            _fileSystemService.CopyFile(sourceFile.FullPath, targetFilePath, overwrite);

            // Preserve file timestamps if possible
            try
            {
                var sourceInfo = new FileInfo(sourceFile.FullPath);
                var targetInfo = new FileInfo(targetFilePath);
                targetInfo.LastWriteTime = sourceInfo.LastWriteTime;
                targetInfo.CreationTime = sourceInfo.CreationTime;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to preserve timestamps for {TargetPath}", targetFilePath);
                // Don't fail the copy operation for timestamp preservation failures
            }

            var duration = DateTime.UtcNow - startTime;

            _logger.LogDebug("Successfully copied {SourcePath} to {TargetPath} in {Duration}ms",
                sourceFile.FullPath, targetFilePath, duration.TotalMilliseconds);

            return Task.FromResult(new CopiedFile
            {
                SourcePath = sourceFile.FullPath,
                TargetPath = targetFilePath,
                SizeInBytes = sourceFile.SizeInBytes,
                CopyDuration = duration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file from {SourcePath} to {TargetPath}", sourceFile.FullPath, targetFilePath);
            throw new FileAccessException($"Failed to copy file: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CopyValidationResult> ValidateCopyOperationAsync(
        string sourcePath,
        string targetPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be null or empty.", nameof(sourcePath));

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        var startTime = DateTime.UtcNow;
        var errors = new List<string>();
        var warnings = new List<string>();
        var conflictingFiles = new List<FileConflict>();

        try
        {
            // Check if source path exists
            if (!_fileSystemService.DirectoryExists(sourcePath))
            {
                errors.Add($"Source directory does not exist: {sourcePath}");
                return CopyValidationResult.Failure(sourcePath, targetPath, errors, validationDuration: DateTime.UtcNow - startTime);
            }

            // Discover template files
            var templateFiles = await _discoveryService.DiscoverTemplatesAsync(sourcePath, recursive: true, cancellationToken);
            
            if (templateFiles.Count == 0)
            {
                warnings.Add("No template files found in source directory");
            }

            // Check target path accessibility
            try
            {
                var targetDir = _fileSystemService.DirectoryExists(targetPath) ? targetPath : Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir) && !_fileSystemService.DirectoryExists(targetDir))
                {
                    // Check if we can create the target directory
                    var parentDir = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(parentDir) && !_fileSystemService.DirectoryExists(parentDir))
                    {
                        errors.Add($"Cannot create target directory, parent directory does not exist: {parentDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot access target path: {ex.Message}");
            }

            // Check for file conflicts
            int filesToOverwrite = 0;
            foreach (var templateFile in templateFiles)
            {
                var targetFilePath = GetTargetFilePath(templateFile, targetPath, preserveStructure: true, sourcePath);
                
                if (_fileSystemService.FileExists(targetFilePath))
                {
                    filesToOverwrite++;
                    
                    if (!overwrite)
                    {
                        var targetInfo = new FileInfo(targetFilePath);
                        var conflict = new FileConflict
                        {
                            SourceFilePath = templateFile.FullPath,
                            TargetFilePath = targetFilePath,
                            ConflictType = targetInfo.IsReadOnly ? ConflictType.ReadOnlyFile : ConflictType.FileExists,
                            SourceSizeBytes = templateFile.SizeInBytes,
                            TargetSizeBytes = targetInfo.Length,
                            SourceLastModified = templateFile.LastModified,
                            TargetLastModified = targetInfo.LastWriteTime
                        };
                        
                        conflictingFiles.Add(conflict);
                    }
                }
            }

            if (conflictingFiles.Count > 0 && !overwrite)
            {
                errors.Add($"Found {conflictingFiles.Count} file conflicts. Use --force to overwrite existing files.");
            }

            // Calculate space requirements
            var totalSize = templateFiles.Sum(f => f.SizeInBytes);
            var directoriesToCreate = templateFiles
                .Select(f => Path.GetDirectoryName(GetTargetFilePath(f, targetPath, preserveStructure: true, sourcePath)))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .Count(d => !_fileSystemService.DirectoryExists(d!));

            // Get available disk space
            long availableSpace = 0;
            try
            {
                var targetDrive = Path.GetPathRoot(Path.GetFullPath(targetPath));
                if (!string.IsNullOrEmpty(targetDrive))
                {
                    var driveInfo = new DriveInfo(targetDrive);
                    availableSpace = driveInfo.AvailableFreeSpace;
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not determine available disk space: {ex.Message}");
            }

            // Check disk space
            var estimatedSpace = totalSize + (directoriesToCreate * 4096); // Rough estimate
            if (availableSpace > 0 && estimatedSpace > availableSpace)
            {
                errors.Add($"Insufficient disk space. Required: {FormatBytes(estimatedSpace)}, Available: {FormatBytes(availableSpace)}");
            }

            var duration = DateTime.UtcNow - startTime;

            if (errors.Count > 0)
            {
                return CopyValidationResult.Failure(sourcePath, targetPath, errors, templateFiles.Count, conflictingFiles, duration, warnings);
            }

            return CopyValidationResult.Success(
                sourcePath,
                targetPath,
                templateFiles.Count,
                filesToOverwrite,
                directoriesToCreate,
                totalSize,
                availableSpace,
                duration,
                warnings);
        }
        catch (Exception ex)
        {
            errors.Add($"Validation failed: {ex.Message}");
            var duration = DateTime.UtcNow - startTime;
            return CopyValidationResult.Failure(sourcePath, targetPath, errors, validationDuration: duration);
        }
    }

    /// <inheritdoc />
    public async Task<CopySpaceEstimate> EstimateCopySpaceAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        bool preserveStructure = true)
    {
        if (templateFiles == null)
            throw new ArgumentNullException(nameof(templateFiles));

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        // Get target drive information
        string fileSystemType = "Unknown";
        int clusterSize = 4096; // Default cluster size
        long availableSpace = 0;

        try
        {
            var targetDrive = Path.GetPathRoot(Path.GetFullPath(targetPath));
            if (!string.IsNullOrEmpty(targetDrive))
            {
                var driveInfo = new DriveInfo(targetDrive);
                availableSpace = driveInfo.AvailableFreeSpace;
                fileSystemType = driveInfo.DriveFormat;
                
                // Estimate cluster size based on file system type
                clusterSize = fileSystemType.ToUpperInvariant() switch
                {
                    "NTFS" => 4096,
                    "FAT32" => 4096,
                    "EXFAT" => 32768,
                    "EXT4" => 4096,
                    "APFS" => 4096,
                    _ => 4096
                };
            }
        }
        catch (Exception ex)
        {
            // Log warning but continue with defaults
            _logger.LogWarning(ex, "Could not determine file system information for {TargetPath}", targetPath);
        }

        return await Task.FromResult(CopySpaceEstimate.Create(
            templateFiles,
            targetPath,
            preserveStructure,
            fileSystemType,
            clusterSize,
            availableSpace,
            safetyBufferPercentage: 0.1));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> CreateDirectoryStructureAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string targetPath,
        CancellationToken cancellationToken = default)
    {
        if (templateFiles == null)
            throw new ArgumentNullException(nameof(templateFiles));

        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        var createdDirectories = new List<string>();

        try
        {
            // Ensure target directory exists
            if (!_fileSystemService.DirectoryExists(targetPath))
            {
                _fileSystemService.CreateDirectory(targetPath);
                createdDirectories.Add(targetPath);
            }

            // Get all unique directory paths
            var directoriesToCreate = templateFiles
                .Select(f => Path.GetDirectoryName(GetTargetFilePath(f, targetPath, preserveStructure: true, null)))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .Where(d => !_fileSystemService.DirectoryExists(d!))
                .ToList();

            foreach (var directory in directoriesToCreate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    _fileSystemService.CreateDirectory(directory!);
                    createdDirectories.Add(directory!);
                    _logger.LogDebug("Created directory: {DirectoryPath}", directory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directory);
                    throw new FileAccessException($"Failed to create directory: {directory}. {ex.Message}", ex);
                }
            }

            return Task.FromResult<IReadOnlyList<string>>(createdDirectories.AsReadOnly());
        }
        catch (Exception ex) when (ex is not FileAccessException)
        {
            _logger.LogError(ex, "Failed to create directory structure for {TargetPath}", targetPath);
            throw new FileAccessException($"Failed to create directory structure: {ex.Message}", ex);
        }
    }

    private string GetTargetFilePath(TemplateFile templateFile, string targetPath, bool preserveStructure, string? sourcePath = null)
    {
        if (preserveStructure)
        {
            // If we have a source path, include the source directory name to maintain top-level folder structure
            if (!string.IsNullOrEmpty(sourcePath))
            {
                var sourceDirectoryName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return Path.Combine(targetPath, sourceDirectoryName, templateFile.RelativePath);
            }
            else
            {
                // Fallback to original behavior when source path is not available
                return Path.Combine(targetPath, templateFile.RelativePath);
            }
        }
        else
        {
            return Path.Combine(targetPath, templateFile.FileName);
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
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