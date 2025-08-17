using System.ComponentModel.DataAnnotations;
using System.Globalization;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for discovering template sets from the file system
/// </summary>
public class TemplateSetDiscoveryService : ITemplateSetService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<TemplateSetDiscoveryService> _logger;

    public TemplateSetDiscoveryService(
        IFileSystemService fileSystemService,
        ILogger<TemplateSetDiscoveryService> logger)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateSet>> ListTemplateSetsAsync(
        string templatesRootPath,
        bool includeEmptyFolders = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatesRootPath))
            throw new ArgumentException("Templates root path cannot be null or empty.", nameof(templatesRootPath));

        if (!_fileSystemService.DirectoryExists(templatesRootPath))
            throw new TemplateNotFoundException($"Templates root path not found: {templatesRootPath}");

        try
        {
            var templateSets = new List<TemplateSet>();
            var directories = _fileSystemService.EnumerateDirectories(templatesRootPath).ToList();

            foreach (var directory in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var directoryName = Path.GetFileName(directory);

                // Skip hidden directories
                if (directoryName.StartsWith('.'))
                {
                    _logger.LogDebug("Skipping hidden directory: {Directory}", directory);
                    continue;
                }

                try
                {
                    var templateSet = await GetTemplateSetFromPathAsync(directory, cancellationToken);
                    
                    if (templateSet.TemplateCount > 0 || includeEmptyFolders)
                    {
                        templateSets.Add(templateSet);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process template set directory: {Directory}", directory);
                }
            }

            return templateSets.AsReadOnly();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied to templates root path: {templatesRootPath}", ex);
        }
        catch (Exception ex) when (ex is not TemplateNotFoundException && ex is not FileAccessException)
        {
            throw new FileAccessException($"Error accessing templates root path: {templatesRootPath}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<TemplateSet> GetTemplateSetAsync(
        string templatesRootPath,
        string setName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatesRootPath))
            throw new ArgumentException("Templates root path cannot be null or empty.", nameof(templatesRootPath));
        
        if (string.IsNullOrWhiteSpace(setName))
            throw new ArgumentException("Set name cannot be null or empty.", nameof(setName));

        var templateSetPath = Path.Combine(templatesRootPath, setName);
        return await GetTemplateSetFromPathAsync(templateSetPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TemplateSet> GetTemplateSetFromPathAsync(
        string templateSetPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateSetPath))
            throw new ArgumentException("Template set path cannot be null or empty.", nameof(templateSetPath));

        if (!_fileSystemService.DirectoryExists(templateSetPath))
            throw new TemplateNotFoundException($"Template set path not found: {templateSetPath}");

        try
        {
            var templates = new List<Template>();
            var docxFiles = await DiscoverDocxFilesAsync(templateSetPath, cancellationToken);
            
            long totalSize = 0;
            DateTime lastModified = DateTime.MinValue;
            bool hasSubfolders = false;

            foreach (var docxFile in docxFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip temporary files
                var fileName = Path.GetFileName(docxFile);
                if (fileName.StartsWith("~$"))
                {
                    _logger.LogDebug("Skipping temporary file: {File}", docxFile);
                    continue;
                }

                var fileSize = _fileSystemService.GetFileSize(docxFile);
                var lastWriteTime = _fileSystemService.GetLastWriteTime(docxFile);
                var relativePath = Path.GetRelativePath(templateSetPath, docxFile);
                
                // Check for subfolders
                if (relativePath.Contains(Path.DirectorySeparatorChar))
                {
                    hasSubfolders = true;
                }

                var template = new Template
                {
                    FileName = fileName,
                    FullPath = docxFile,
                    RelativePath = relativePath,
                    SizeBytes = fileSize,
                    LastModified = lastWriteTime.ToUniversalTime()
                };

                templates.Add(template);
                totalSize += fileSize;
                
                if (lastWriteTime.ToUniversalTime() > lastModified)
                {
                    lastModified = lastWriteTime.ToUniversalTime();
                }
            }

            var setName = Path.GetFileName(templateSetPath);
            
            return new TemplateSet
            {
                Name = setName,
                FullPath = templateSetPath,
                Templates = templates.AsReadOnly(),
                TemplateCount = templates.Count,
                TotalSizeBytes = totalSize,
                LastModified = lastModified == DateTime.MinValue ? DateTime.UtcNow : lastModified,
                HasSubfolders = hasSubfolders
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new FileAccessException($"Access denied to template set path: {templateSetPath}", ex);
        }
        catch (Exception ex) when (ex is not TemplateNotFoundException && ex is not FileAccessException)
        {
            throw new FileAccessException($"Error accessing template set path: {templateSetPath}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<TemplateSetValidationResult> ValidateTemplateSetAsync(
        string templatesRootPath,
        string setName,
        int requireMinimumFiles = 1,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var templateSet = await GetTemplateSetAsync(templatesRootPath, setName, cancellationToken);
            
            // Validate minimum file count
            if (templateSet.TemplateCount < requireMinimumFiles)
            {
                errors.Add($"Template set has {templateSet.TemplateCount} files but requires at least {requireMinimumFiles}");
            }

            // Validate directory exists
            if (!templateSet.DirectoryExists())
            {
                errors.Add($"Template set directory does not exist: {templateSet.FullPath}");
            }

            // Validate template set integrity
            if (!templateSet.IsValid())
            {
                errors.Add("Template set data integrity check failed");
            }

            // Check for non-existent files (warning)
            var nonExistentFiles = templateSet.Templates.Where(t => !File.Exists(t.FullPath)).ToList();
            if (nonExistentFiles.Any())
            {
                warnings.Add($"{nonExistentFiles.Count} file(s) not found on disk");
            }

            // Check for very large files (warning)
            const long largeFileThreshold = 10 * 1024 * 1024; // 10 MB
            var largeFiles = templateSet.Templates.Where(t => t.SizeBytes > largeFileThreshold).ToList();
            if (largeFiles.Any())
            {
                warnings.Add($"{largeFiles.Count} large file(s) (>10 MB) found in template set");
            }

            var duration = DateTime.UtcNow - startTime;
            
            return errors.Count == 0
                ? TemplateSetValidationResult.Success(templateSet.TemplateCount, duration, warnings.AsReadOnly())
                : TemplateSetValidationResult.Failure(errors.AsReadOnly(), templateSet.TemplateCount, duration, warnings.AsReadOnly());
        }
        catch (Exception ex)
        {
            errors.Add($"Validation failed: {ex.Message}");
            var duration = DateTime.UtcNow - startTime;
            return TemplateSetValidationResult.Failure(errors.AsReadOnly(), 0, duration, warnings.AsReadOnly());
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, TemplateSetValidationResult>> ValidateTemplateSetsAsync(
        IReadOnlyList<TemplateSet> templateSets,
        int requireMinimumFiles = 1,
        CancellationToken cancellationToken = default)
    {
        if (templateSets == null)
            throw new ArgumentNullException(nameof(templateSets));

        var results = new Dictionary<string, TemplateSetValidationResult>();

        foreach (var templateSet in templateSets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ValidateTemplateSetAsync(
                Path.GetDirectoryName(templateSet.FullPath)!,
                templateSet.Name,
                requireMinimumFiles,
                cancellationToken);

            results[templateSet.Name] = result;
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateSet>> SearchTemplateSetsAsync(
        string templatesRootPath,
        IReadOnlyList<string> filePatterns,
        bool requireAllPatterns = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatesRootPath))
            throw new ArgumentException("Templates root path cannot be null or empty.", nameof(templatesRootPath));

        if (filePatterns == null || filePatterns.Count == 0)
            throw new ArgumentException("File patterns cannot be null or empty.", nameof(filePatterns));

        var allTemplateSets = await ListTemplateSetsAsync(templatesRootPath, false, cancellationToken);
        var matchingSets = new List<TemplateSet>();

        foreach (var templateSet in allTemplateSets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matchedPatterns = new HashSet<string>();

            foreach (var template in templateSet.Templates)
            {
                foreach (var pattern in filePatterns)
                {
                    if (IsPatternMatch(template.FileName, pattern) || IsPatternMatch(template.RelativePath, pattern))
                    {
                        matchedPatterns.Add(pattern);
                    }
                }
            }

            if ((requireAllPatterns && matchedPatterns.Count == filePatterns.Count) ||
                (!requireAllPatterns && matchedPatterns.Count > 0))
            {
                matchingSets.Add(templateSet);
            }
        }

        return matchingSets.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<TemplateSetStatistics> GetTemplateSetStatisticsAsync(
        string templatesRootPath,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var templateSets = await ListTemplateSetsAsync(templatesRootPath, false, cancellationToken);
        
        var largestByCount = templateSets.OrderByDescending(s => s.TemplateCount).FirstOrDefault();
        var largestBySize = templateSets.OrderByDescending(s => s.TotalSizeBytes).FirstOrDefault();
        var mostRecent = templateSets.OrderByDescending(s => s.LastModified).FirstOrDefault();
        
        // Create distribution
        var distribution = new Dictionary<string, int>
        {
            ["0-5 files"] = templateSets.Count(s => s.TemplateCount <= 5),
            ["6-10 files"] = templateSets.Count(s => s.TemplateCount > 5 && s.TemplateCount <= 10),
            ["11-20 files"] = templateSets.Count(s => s.TemplateCount > 10 && s.TemplateCount <= 20),
            ["20+ files"] = templateSets.Count(s => s.TemplateCount > 20)
        };
        
        // Find common patterns
        var allFileNames = templateSets.SelectMany(s => s.Templates.Select(t => t.FileName)).ToList();
        var patterns = new List<FilePattern>();
        if (allFileNames.Any())
        {
            var docxPattern = new FilePattern
            {
                Pattern = "*.docx",
                OccurrenceCount = allFileNames.Count,
                Percentage = 100.0
            };
            patterns.Add(docxPattern);
        }
        
        var emptyOrInvalid = templateSets.Where(s => !s.IsValid() || s.TemplateCount == 0)
                                        .Select(s => s.Name)
                                        .ToList();
        
        var duration = DateTime.UtcNow - startTime;
        
        return new TemplateSetStatistics
        {
            TemplateSetCount = templateSets.Count,
            TotalTemplateCount = templateSets.Sum(s => s.TemplateCount),
            TotalSizeBytes = templateSets.Sum(s => s.TotalSizeBytes),
            SetsWithSubfolders = templateSets.Count(s => s.HasSubfolders),
            LargestSetByCount = largestByCount != null ? TemplateSetSummary.FromTemplateSet(largestByCount) 
                : new TemplateSetSummary { Name = "None", TemplateCount = 0, TotalSizeBytes = 0, LastModified = DateTime.UtcNow },
            LargestSetBySize = largestBySize != null ? TemplateSetSummary.FromTemplateSet(largestBySize)
                : new TemplateSetSummary { Name = "None", TemplateCount = 0, TotalSizeBytes = 0, LastModified = DateTime.UtcNow },
            MostRecentSet = mostRecent != null ? TemplateSetSummary.FromTemplateSet(mostRecent)
                : new TemplateSetSummary { Name = "None", TemplateCount = 0, TotalSizeBytes = 0, LastModified = DateTime.UtcNow },
            TemplateCountDistribution = distribution,
            CommonFilePatterns = patterns.AsReadOnly(),
            EmptyOrInvalidSets = emptyOrInvalid.AsReadOnly(),
            AnalysisDuration = duration
        };
    }

    /// <inheritdoc />
    public Task<CopyResult> CreateTemplateSetCopyAsync(
        string sourceSetPath,
        string targetSetPath,
        bool preserveStructure = true,
        CancellationToken cancellationToken = default)
    {
        // This will be implemented in a later story
        throw new NotImplementedException("Template set copy functionality will be implemented in story 02.004");
    }

    /// <inheritdoc />
    public bool IsValidTemplateSetName(string setName)
    {
        if (string.IsNullOrWhiteSpace(setName))
            return false;

        // Check for invalid characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (setName.Any(c => invalidChars.Contains(c)))
            return false;

        // Check for reserved names (Windows)
        string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", 
                                  "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", 
                                  "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        
        if (reservedNames.Contains(setName.ToUpperInvariant()))
            return false;

        // Check length
        if (setName.Length > 255)
            return false;

        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSuggestedTemplateSetNamesAsync(
        string templatesRootPath,
        string baseName,
        int maxSuggestions = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("Base name cannot be null or empty.", nameof(baseName));

        var suggestions = new List<string>();
        var existingSets = await ListTemplateSetsAsync(templatesRootPath, true, cancellationToken);
        var existingNames = existingSets.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Clean up base name
        baseName = baseName.Trim();
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(c.ToString(), "_");
        }

        // Generate suggestions
        if (!existingNames.Contains(baseName))
        {
            suggestions.Add(baseName);
        }

        // Add numbered suggestions
        int counter = 1;
        while (suggestions.Count < maxSuggestions)
        {
            var suggestion = $"{baseName}_{counter}";
            if (!existingNames.Contains(suggestion))
            {
                suggestions.Add(suggestion);
            }
            counter++;
            
            // Prevent infinite loop
            if (counter > 100)
                break;
        }

        // Add date-based suggestions
        if (suggestions.Count < maxSuggestions)
        {
            var dateStr = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var dateSuggestion = $"{baseName}_{dateStr}";
            if (!existingNames.Contains(dateSuggestion))
            {
                suggestions.Add(dateSuggestion);
            }
        }

        return suggestions.Take(maxSuggestions).ToList().AsReadOnly();
    }

    private async Task<IEnumerable<string>> DiscoverDocxFilesAsync(
        string directory, 
        CancellationToken cancellationToken)
    {
        var docxFiles = new List<string>();

        try
        {
            // Get all .docx files in current directory
            var files = _fileSystemService.EnumerateFiles(directory, "*.docx");
            docxFiles.AddRange(files);

            // Recursively search subdirectories
            var subdirectories = _fileSystemService.EnumerateDirectories(directory);
            foreach (var subdirectory in subdirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var subdirName = Path.GetFileName(subdirectory);
                // Skip hidden directories
                if (!subdirName.StartsWith('.'))
                {
                    var subdirFiles = await DiscoverDocxFilesAsync(subdirectory, cancellationToken);
                    docxFiles.AddRange(subdirFiles);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Directory}", directory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing directory: {Directory}", directory);
        }

        return docxFiles;
    }

    private bool IsPatternMatch(string fileName, string pattern)
    {
        // Simple wildcard pattern matching
        pattern = pattern.Replace("*", ".*").Replace("?", ".");
        return System.Text.RegularExpressions.Regex.IsMatch(
            fileName, 
            $"^{pattern}$", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}