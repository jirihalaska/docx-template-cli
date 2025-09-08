using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using DocxTemplate.Processing;
using DocxTemplate.Processing.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for replacing placeholders in Word documents with actual values.
/// Now uses the unified PlaceholderReplacementEngine for all processing operations.
/// </summary>
public class PlaceholderReplaceService : IPlaceholderReplaceService
{
    private readonly ILogger<PlaceholderReplaceService> _logger;
    private readonly IErrorHandler _errorHandler;
    private readonly IFileSystemService _fileSystemService;
    private readonly PlaceholderReplacementEngine _replacementEngine;

    public PlaceholderReplaceService(
        ILogger<PlaceholderReplaceService> logger,
        IErrorHandler errorHandler,
        IFileSystemService fileSystemService,
        PlaceholderReplacementEngine replacementEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _replacementEngine = replacementEngine ?? throw new ArgumentNullException(nameof(replacementEngine));
    }

    /// <inheritdoc />
    public async Task<ReplaceResult> ReplacePlaceholdersAsync(
        string folderPath,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(replacementMap);

        try
        {

            if (!_fileSystemService.DirectoryExists(folderPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
            }

            var templateFiles = await DiscoverTemplateFilesAsync(folderPath, cancellationToken);

            return await ReplacePlaceholdersAsync(templateFiles, replacementMap, createBackup, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "folder replacement");
            var fileResult = FileReplaceResult.Failure(folderPath, errorResult?.Message ?? ex.Message);
            return ReplaceResult.Success([fileResult], TimeSpan.Zero);
        }
    }

    /// <inheritdoc />
    public async Task<ReplaceResult> ReplacePlaceholdersAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFiles);
        ArgumentNullException.ThrowIfNull(replacementMap);

        if (!replacementMap.IsValid())
        {
            throw new ArgumentException("Invalid replacement map", nameof(replacementMap));
        }

        try
        {

            var startTime = DateTime.UtcNow;
            var results = new List<FileReplaceResult>();

            // Create backups if requested and files exist
            if (createBackup && templateFiles.Count > 0)
            {
                var backupResult = await CreateBackupsAsync(templateFiles, cancellationToken: cancellationToken);
                if (!backupResult.IsCompletelySuccessful)
                {
                    _logger.LogWarning("Some backups failed, continuing with replacements");
                }
            }

            // Process each file
            foreach (var templateFile in templateFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileResult = await ReplacePlaceholdersInFileAsync(
                    templateFile.FullPath,
                    replacementMap,
                    false, // Already created backup above
                    cancellationToken);

                results.Add(fileResult);
            }

            var duration = DateTime.UtcNow - startTime;
            return ReplaceResult.Success(results, duration);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "batch replacement");
            var fileResult = FileReplaceResult.Failure("", errorResult?.Message ?? ex.Message);
            return ReplaceResult.Success([fileResult], TimeSpan.Zero);
        }
    }

    /// <inheritdoc />
    public async Task<FileReplaceResult> ReplacePlaceholdersInFileAsync(
        string templatePath,
        ReplacementMap replacementMap,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentNullException.ThrowIfNull(replacementMap);

        if (!replacementMap.IsValid())
        {
            throw new ArgumentException("Invalid replacement map", nameof(replacementMap));
        }

        try
        {

            if (!_fileSystemService.FileExists(templatePath))
            {
                return FileReplaceResult.Failure(templatePath, "File not found");
            }

            var startTime = DateTime.UtcNow;
            var backupPath = string.Empty;

            if (createBackup)
            {
                backupPath = await CreateFileBackupAsync(templatePath, cancellationToken);
            }

            var result = await _replacementEngine.ProcessDocumentAsync(templatePath, ProcessingMode.Replace, replacementMap, cancellationToken);
            var replacementCount = result.ReplacementsPerformed;
            
            // Convert detailed replacements to DetailedReplacement objects
            var fileName = Path.GetFileName(templatePath);
            var detailedReplacements = result.DetailedReplacements
                .Select(kvp =>
                {
                    var parts = kvp.Key.Split('→', 2);
                    var placeholderName = parts.Length > 0 ? parts[0] : kvp.Key;
                    var replacedValue = parts.Length > 1 ? parts[1] : "";
                    return DetailedReplacement.Create(fileName, placeholderName, replacedValue, kvp.Value);
                })
                .ToList();
            
            // Convert unreplaced placeholders to UnreplacedPlaceholder objects
            var unreplacedPlaceholders = result.UnreplacedPlaceholders
                .Select(kvp => UnreplacedPlaceholder.Create(fileName, kvp.Key, kvp.Value))
                .ToList();
            
            // File prefix is now handled by TemplateCopyService during copy operation
            var finalFilePath = templatePath;
            
            var endTime = DateTime.UtcNow;

            return FileReplaceResult.Success(
                finalFilePath,
                replacementCount,
                backupPath,
                endTime - startTime,
                _fileSystemService.GetFileSize(finalFilePath),
                detailedReplacements,
                unreplacedPlaceholders);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "file replacement");
            return FileReplaceResult.Failure(templatePath, errorResult?.Message ?? ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ReplacementPreview> PreviewReplacementsAsync(
        string folderPath,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(replacementMap);

        try
        {

            if (!_fileSystemService.DirectoryExists(folderPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
            }

            var startTime = DateTime.UtcNow;
            var templateFiles = await DiscoverTemplateFilesAsync(folderPath, cancellationToken);
            var previews = new List<FileReplacementPreview>();
            var allPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var templateFile in templateFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var filePreview = await CreateFilePreviewAsync(templateFile.FullPath, replacementMap, cancellationToken);
                previews.Add(filePreview);

                // Collect all placeholders for summary
                foreach (var detail in filePreview.ReplacementDetails)
                {
                    allPlaceholders.Add(detail.PlaceholderName);
                }
            }

            var mappedPlaceholders = allPlaceholders.Where(p => replacementMap.Mappings.ContainsKey(p)).ToList();
            var unmappedPlaceholders = allPlaceholders.Except(mappedPlaceholders).ToList();
            var unusedMappings = replacementMap.PlaceholderNames.Except(allPlaceholders).ToList();

            var duration = DateTime.UtcNow - startTime;

            return ReplacementPreview.Create(previews, mappedPlaceholders, unmappedPlaceholders, unusedMappings, duration);
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "replacement preview");
            throw new InvalidOperationException($"Preview failed: {errorResult?.Message ?? ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public ReplacementValidationResult ValidateReplacements(
        IReadOnlyList<Placeholder> placeholders,
        ReplacementMap replacementMap,
        bool requireAllPlaceholders = false)
    {
        ArgumentNullException.ThrowIfNull(placeholders);
        ArgumentNullException.ThrowIfNull(replacementMap);

        try
        {

            var startTime = DateTime.UtcNow;
            var errors = new List<string>();
            var warnings = new List<string>();
            var invalidMappings = new List<InvalidMapping>();

            // Check for placeholders without replacements
            var placeholderNames = placeholders.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var mappingNames = replacementMap.PlaceholderNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var validMappings = placeholderNames.Intersect(mappingNames).ToList();
            var missingReplacements = placeholderNames.Except(mappingNames).ToList();
            var unusedMappings = mappingNames.Except(placeholderNames).ToList();

            var missingRequired = requireAllPlaceholders ? missingReplacements : [];
            var missingOptional = requireAllPlaceholders ? [] : missingReplacements;

            if (requireAllPlaceholders && missingReplacements.Count > 0)
            {
                errors.AddRange(missingReplacements.Select(name => $"Missing replacement for required placeholder: {name}"));
            }
            else if (missingReplacements.Count > 0)
            {
                warnings.AddRange(missingReplacements.Select(name => $"No replacement provided for placeholder: {name}"));
            }

            if (unusedMappings.Count > 0)
            {
                warnings.AddRange(unusedMappings.Select(name => $"Unused replacement mapping: {name}"));
            }

            // Validate replacement values
            foreach (var mapping in replacementMap.Mappings)
            {
                if (!ReplacementMap.IsValidPlaceholderName(mapping.Key))
                {
                    errors.Add($"Invalid placeholder name: {mapping.Key}");
                    invalidMappings.Add(new InvalidMapping
                    {
                        PlaceholderName = mapping.Key,
                        ReplacementValue = mapping.Value,
                        ValidationError = "Invalid placeholder name format",
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var isValid = errors.Count == 0;

            if (isValid)
            {
                return ReplacementValidationResult.Success(
                    validMappings,
                    missingOptional,
                    unusedMappings,
                    duration,
                    warnings);
            }
            else
            {
                return ReplacementValidationResult.Failure(
                    errors,
                    validMappings,
                    missingRequired,
                    missingOptional,
                    unusedMappings,
                    invalidMappings,
                    duration,
                    warnings);
            }
        }
        catch (Exception ex)
        {
            return ReplacementValidationResult.Failure(
                [$"Validation failed: {ex.Message}"]);
        }
    }

    /// <inheritdoc />
    public async Task<BackupResult> CreateBackupsAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string? backupDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFiles);

        try
        {

            if (templateFiles.Count == 0)
            {
                return BackupResult.Success([], "", TimeSpan.Zero);
            }

            var startTime = DateTime.UtcNow;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var actualBackupDirectory = backupDirectory ??
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(templateFiles[0].FullPath)!, $"backup_{timestamp}");

            _fileSystemService.CreateDirectory(actualBackupDirectory);

            var backupDetails = new List<BackupDetail>();
            var errors = new List<BackupError>();
            var failedCount = 0;

            foreach (var templateFile in templateFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var fileName = System.IO.Path.GetFileName(templateFile.FullPath);
                    var backupPath = System.IO.Path.Combine(actualBackupDirectory, fileName);

                    _fileSystemService.CopyFile(templateFile.FullPath, backupPath);

                    backupDetails.Add(new BackupDetail
                    {
                        SourcePath = templateFile.FullPath,
                        BackupPath = backupPath,
                        SizeBytes = templateFile.SizeInBytes
                    });

                    _logger.LogDebug("Created backup: {BackupPath}", backupPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to backup file: {FilePath}", templateFile.FullPath);
                    failedCount++;
                    errors.Add(new BackupError
                    {
                        SourcePath = templateFile.FullPath,
                        BackupPath = System.IO.Path.Combine(actualBackupDirectory, System.IO.Path.GetFileName(templateFile.FullPath)),
                        Message = ex.Message,
                        ExceptionType = ex.GetType().Name
                    });
                }
            }

            var duration = DateTime.UtcNow - startTime;

            if (failedCount == 0)
            {
                return BackupResult.Success(backupDetails, actualBackupDirectory, duration);
            }
            else
            {
                return BackupResult.WithFailures(backupDetails, actualBackupDirectory, duration, failedCount, errors);
            }
        }
        catch (Exception ex)
        {
            var errorResult = await _errorHandler.HandleExceptionAsync(ex, "backup creation");
            return BackupResult.WithFailures(
                [],
                "",
                TimeSpan.Zero,
                1,
                [new BackupError { SourcePath = "", BackupPath = "", Message = errorResult?.Message ?? ex.Message }]);
        }
    }

    private Task<IReadOnlyList<TemplateFile>> DiscoverTemplateFilesAsync(string folderPath, CancellationToken cancellationToken)
    {
        var files = _fileSystemService.EnumerateFiles(folderPath, "*.docx", SearchOption.AllDirectories);
        var templateFiles = new List<TemplateFile>();

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var fileName = _fileSystemService.GetFileName(filePath);
                var relativePath = System.IO.Path.GetRelativePath(folderPath, filePath);
                var fileSize = _fileSystemService.GetFileSize(filePath);
                var lastModified = _fileSystemService.GetLastWriteTime(filePath);

                var templateFile = new TemplateFile
                {
                    FullPath = filePath,
                    RelativePath = relativePath,
                    FileName = fileName,
                    SizeInBytes = fileSize,
                    LastModified = lastModified
                };

                templateFiles.Add(templateFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process template file: {FilePath}", filePath);
            }
        }

        return Task.FromResult<IReadOnlyList<TemplateFile>>(templateFiles);
    }

    private async Task<string> CreateFileBackupAsync(string filePath, CancellationToken cancellationToken)
    {
        var backupPath = await _fileSystemService.CreateBackupAsync(filePath, cancellationToken);
        _logger.LogDebug("Created backup: {BackupPath}", backupPath);
        return backupPath;
    }


    private async Task<FileReplacementPreview> CreateFilePreviewAsync(
        string filePath,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken)
    {
        try
        {
            // Use the replacement engine to scan for placeholders
            var scanResult = await _replacementEngine.ProcessDocumentAsync(
                filePath, 
                ProcessingMode.Scan, 
                null, // No replacement map needed for scanning
                cancellationToken);

            var placeholderCounts = scanResult.DiscoveredPlaceholders
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var replacementDetails = placeholderCounts.Select(kvp =>
            {
                var placeholderName = kvp.Key;
                var count = kvp.Value;
                var hasReplacement = replacementMap.Mappings.TryGetValue(placeholderName, out var replacement);

                return new ReplacementDetail
                {
                    PlaceholderName = placeholderName,
                    CurrentValue = $"{{{{{placeholderName}}}}}",
                    NewValue = replacement ?? string.Empty,
                    OccurrenceCount = count
                };
            }).ToList();

            var totalReplacements = replacementDetails.Where(r => !string.IsNullOrEmpty(r.NewValue)).Sum(r => r.OccurrenceCount);

            return new FileReplacementPreview
            {
                FilePath = filePath,
                ReplacementCount = totalReplacements,
                CanProcess = true,
                ReplacementDetails = replacementDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preview file: {FilePath}", filePath);
            return new FileReplacementPreview
            {
                FilePath = filePath,
                ReplacementCount = 0,
                CanProcess = false,
                ErrorMessage = ex.Message,
                ReplacementDetails = []
            };
        }
    }

    /// <inheritdoc />
    public async Task<ReplaceResult> ReplaceInTemplateAsync(
        TemplateFile templateFile,
        ReplacementMap replacementMap,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateFile);
        ArgumentNullException.ThrowIfNull(replacementMap);

        var startTime = DateTime.UtcNow;

        try
        {
            var result = await _replacementEngine.ProcessDocumentAsync(templateFile.FullPath, ProcessingMode.Replace, replacementMap, cancellationToken);
            var replacementCount = result.ReplacementsPerformed;

            // File prefix is now handled by TemplateCopyService during copy operation
            var finalFilePath = templateFile.FullPath;

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var fileResult = FileReplaceResult.Success(
                finalFilePath,
                replacementCount,
                processingDuration: duration);

            return ReplaceResult.Success(
                [fileResult],
                duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace placeholders in template {TemplatePath}", templateFile.FullPath);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var fileResult = FileReplaceResult.Failure(
                templateFile.FullPath,
                ex.Message,
                duration);

            return ReplaceResult.Success(
                [fileResult],
                duration);
        }
    }



    /// <summary>
    /// Gets a unique file path by adding numeric suffix if file already exists
    /// </summary>
    /// <param name="originalPath">The desired file path</param>
    /// <returns>Unique file path</returns>
    private string GetUniqueFilePath(string originalPath)
    {
        if (!_fileSystemService.FileExists(originalPath))
            return originalPath;
        
        var directory = System.IO.Path.GetDirectoryName(originalPath) ?? throw new InvalidOperationException("Could not determine directory");
        var fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
        var extension = System.IO.Path.GetExtension(originalPath);
        
        var counter = 1;
        string newPath;
        
        do
        {
            var newFileName = $"{fileName}({counter}){extension}";
            newPath = System.IO.Path.Combine(directory, newFileName);
            counter++;
        } 
        while (_fileSystemService.FileExists(newPath) && counter < 1000); // Prevent infinite loop
        
        return newPath;
    }
}
