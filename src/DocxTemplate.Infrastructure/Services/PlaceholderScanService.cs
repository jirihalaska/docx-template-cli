using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.DocxProcessing;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for scanning DOCX templates to discover placeholder patterns using OpenXML
/// </summary>
public class PlaceholderScanService : IPlaceholderScanService
{
    private readonly ITemplateDiscoveryService _discoveryService;
    private readonly ILogger<PlaceholderScanService> _logger;
    private readonly PlaceholderScanner _placeholderScanner;

    public PlaceholderScanService(
        ITemplateDiscoveryService discoveryService,
        ILogger<PlaceholderScanService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create a logger for the scanner using the factory pattern
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _placeholderScanner = new PlaceholderScanner(loggerFactory.CreateLogger<PlaceholderScanner>());
    }

    /// <inheritdoc />
    public async Task<PlaceholderScanResult> ScanPlaceholdersAsync(
        string folderPath,
        string pattern = @"\{\{.*?\}\}",
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));

        ValidatePattern(pattern);

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting placeholder scan in folder: {Path}", folderPath);

        try
        {
            var templateFiles = await _discoveryService.DiscoverTemplatesAsync(folderPath, recursive, cancellationToken);
            return await ScanPlaceholdersAsync(templateFiles, pattern, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidPlaceholderPatternException)
        {
            _logger.LogError(ex, "Failed to scan placeholders in folder: {Path}", folderPath);
            var duration = DateTime.UtcNow - startTime;
            
            return PlaceholderScanResult.WithErrors(
                [],
                0,
                duration,
                0,
                0,
                [new ScanError { FilePath = folderPath, Message = ex.Message, ExceptionType = ex.GetType().Name }]);
        }
    }

    /// <inheritdoc />
    public async Task<PlaceholderScanResult> ScanPlaceholdersAsync(
        IReadOnlyList<TemplateFile> templateFiles,
        string pattern = @"\{\{.*?\}\}",
        CancellationToken cancellationToken = default)
    {
        if (templateFiles == null)
            throw new ArgumentNullException(nameof(templateFiles));

        ValidatePattern(pattern);

        var startTime = DateTime.UtcNow;
        var errors = new List<ScanError>();
        var allPlaceholders = new Dictionary<string, List<PlaceholderLocation>>();
        int filesWithPlaceholders = 0;
        int failedFiles = 0;

        _logger.LogInformation("Scanning {Count} template files for placeholders", templateFiles.Count);

        // Use parallel processing for better performance
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var tasks = templateFiles.Select(async (templateFile) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var placeholders = await ScanSingleFileAsync(templateFile.FullPath, pattern, cancellationToken);
                return new ScanFileResult
                {
                    FilePath = templateFile.FullPath,
                    Placeholders = placeholders,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan file: {Path}", templateFile.FullPath);
                return new ScanFileResult
                {
                    FilePath = templateFile.FullPath,
                    Placeholders = [],
                    Error = new ScanError
                    {
                        FilePath = templateFile.FullPath,
                        Message = ex.Message,
                        ExceptionType = ex.GetType().Name
                    }
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Aggregate results
        foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (result.Error != null)
            {
                errors.Add(result.Error);
                failedFiles++;
                continue;
            }

            if (result.Placeholders.Any())
            {
                filesWithPlaceholders++;

                foreach (var placeholder in result.Placeholders)
                {
                    if (!allPlaceholders.ContainsKey(placeholder.Name))
                    {
                        allPlaceholders[placeholder.Name] = [];
                    }

                    allPlaceholders[placeholder.Name].AddRange(placeholder.Locations);
                }
            }
        }

        // Create final placeholder objects with aggregated locations
        var placeholderList = allPlaceholders.Select(kvp =>
        {
            // Check if this is an image placeholder
            if (kvp.Key.StartsWith("image:") && kvp.Key.Contains("|width:") && kvp.Key.Contains("|height:"))
            {
                // Parse image placeholder details
                var imageMatch = Regex.Match(kvp.Key, @"image:([^|]+)\|width:(\d+)\|height:(\d+)");
                if (imageMatch.Success)
                {
                    var imageName = imageMatch.Groups[1].Value;
                    var width = int.Parse(imageMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var height = int.Parse(imageMatch.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    
                    return new Placeholder
                    {
                        Name = imageName,
                        Pattern = PlaceholderPatterns.ImagePlaceholderPattern,
                        Locations = kvp.Value.AsReadOnly(),
                        TotalOccurrences = kvp.Value.Sum(l => l.Occurrences),
                        Type = PlaceholderType.Image,
                        ImageProperties = new ImageProperties
                        {
                            ImageName = imageName,
                            MaxWidth = width,
                            MaxHeight = height
                        }
                    };
                }
            }
            
            // It's a text placeholder
            return new Placeholder
            {
                Name = kvp.Key,
                Pattern = pattern,
                Locations = kvp.Value.AsReadOnly(),
                TotalOccurrences = kvp.Value.Sum(l => l.Occurrences),
                Type = PlaceholderType.Text,
                ImageProperties = null
            };
        }).ToList();

        // Put SOUBOR_PREFIX first if it exists, keep the rest in original order
        var finalPlaceholders = new List<Placeholder>();
        var prefixPlaceholder = placeholderList.FirstOrDefault(p => string.Equals(p.Name, Placeholder.FilePrefixPlaceholder, StringComparison.OrdinalIgnoreCase));
        if (prefixPlaceholder != null)
        {
            finalPlaceholders.Add(prefixPlaceholder);
            finalPlaceholders.AddRange(placeholderList.Where(p => !string.Equals(p.Name, Placeholder.FilePrefixPlaceholder, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            finalPlaceholders.AddRange(placeholderList);
        }

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("Completed placeholder scan. Found {UniqueCount} unique placeholders in {Duration}ms",
            finalPlaceholders.Count, duration.TotalMilliseconds);

        return errors.Count > 0 || failedFiles > 0
            ? PlaceholderScanResult.WithErrors(finalPlaceholders, templateFiles.Count, duration, filesWithPlaceholders, failedFiles, errors)
            : PlaceholderScanResult.Success(finalPlaceholders, templateFiles.Count, duration, filesWithPlaceholders);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Placeholder>> ScanSingleFileAsync(
        string templatePath,
        string pattern = @"\{\{.*?\}\}",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));

        ValidatePattern(pattern);

        if (!File.Exists(templatePath))
            throw new TemplateNotFoundException($"Template file not found: {templatePath}");

        try
        {
            var result = await ScanSingleFileInternalAsync(templatePath, pattern, cancellationToken);
            return result.Placeholders;
        }
        catch (Exception ex) when (ex is not TemplateNotFoundException && ex is not InvalidPlaceholderPatternException)
        {
            throw new DocumentProcessingException($"Failed to process document: {templatePath}", ex);
        }
    }

    /// <inheritdoc />
    public bool ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new InvalidPlaceholderPatternException("Pattern cannot be null or empty");

        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
            
            // Test the regex with a simple example to ensure it works
            var testResult = regex.Match("{{test}}");
            return true;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidPlaceholderPatternException($"Invalid regex pattern: {pattern}", ex);
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new InvalidPlaceholderPatternException($"Regex pattern timeout: {pattern}", ex);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExtractPlaceholderNames(string content, string pattern = @"\{\{.*?\}\}")
    {
        if (string.IsNullOrEmpty(content))
            return [];

        ValidatePattern(pattern);

        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
            var matches = regex.Matches(content);
            
            var names = new HashSet<string>();
            foreach (Match match in matches)
            {
                // Extract the placeholder name by removing common delimiters
                var name = match.Value.Trim('{', '}', '[', ']', '<', '>').Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }

            return names.ToList().AsReadOnly();
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new InvalidPlaceholderPatternException($"Pattern matching timeout: {pattern}", ex);
        }
    }

    /// <inheritdoc />
    public PlaceholderStatistics GetPlaceholderStatistics(PlaceholderScanResult scanResult)
    {
        if (scanResult == null)
            throw new ArgumentNullException(nameof(scanResult));

        var placeholdersByFile = scanResult.Placeholders
            .SelectMany(p => p.Locations.GroupBy(l => l.FilePath)
                .Select(g => new { FilePath = g.Key, PlaceholderCount = g.Sum(l => l.Occurrences) }))
            .GroupBy(x => x.FilePath)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PlaceholderCount));

        var mostCommonPlaceholders = scanResult.Placeholders
            .OrderByDescending(p => p.TotalOccurrences)
            .Take(10)
            .ToList();

        var fileDistribution = placeholdersByFile.Values
            .GroupBy(count => count)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PlaceholderStatistics
        {
            TotalUniquePlaceholders = scanResult.UniquePlaceholderCount,
            TotalOccurrences = scanResult.TotalOccurrences,
            FilesScanned = scanResult.TotalFilesScanned,
            FilesWithPlaceholders = scanResult.FilesWithPlaceholders,
            AveragePerFile = scanResult.AveragePlaceholdersPerFile,
            MostCommonPlaceholders = mostCommonPlaceholders.AsReadOnly(),
            PlaceholderDistribution = fileDistribution,
            ScanDuration = scanResult.ScanDuration
        };
    }

    private async Task<ScanFileResult> ScanSingleFileInternalAsync(
        string templatePath,
        string pattern,
        CancellationToken cancellationToken)
    {
        var placeholders = new Dictionary<string, List<PlaceholderLocation>>();

        try
        {
            using var document = WordprocessingDocument.Open(templatePath, false);
            var mainDocumentPart = document.MainDocumentPart;

            if (mainDocumentPart?.Document?.Body != null)
            {
                // Scan main document body
                await ScanDocumentPartAsync(mainDocumentPart.Document.Body, templatePath, "Body", pattern, placeholders, cancellationToken);
            }

            // Scan headers
            if (mainDocumentPart?.HeaderParts != null)
            {
                int headerIndex = 0;
                foreach (var headerPart in mainDocumentPart.HeaderParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (headerPart.Header != null)
                    {
                        await ScanDocumentPartAsync(headerPart.Header, templatePath, $"Header{headerIndex}", pattern, placeholders, cancellationToken);
                    }
                    headerIndex++;
                }
            }

            // Scan footers
            if (mainDocumentPart?.FooterParts != null)
            {
                int footerIndex = 0;
                foreach (var footerPart in mainDocumentPart.FooterParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (footerPart.Footer != null)
                    {
                        await ScanDocumentPartAsync(footerPart.Footer, templatePath, $"Footer{footerIndex}", pattern, placeholders, cancellationToken);
                    }
                    footerIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            return new ScanFileResult
            {
                FilePath = templatePath,
                Placeholders = [],
                Error = new ScanError
                {
                    FilePath = templatePath,
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name
                }
            };
        }

        // Convert to final placeholder objects
        var placeholderList = placeholders.Select(kvp =>
        {
            // Check if this is an image placeholder
            if (kvp.Key.StartsWith("image:") && kvp.Key.Contains("|width:") && kvp.Key.Contains("|height:"))
            {
                // Parse image placeholder details
                var imageMatch = Regex.Match(kvp.Key, @"image:([^|]+)\|width:(\d+)\|height:(\d+)");
                if (imageMatch.Success)
                {
                    var imageName = imageMatch.Groups[1].Value;
                    var width = int.Parse(imageMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var height = int.Parse(imageMatch.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    
                    return new Placeholder
                    {
                        Name = imageName,
                        Pattern = PlaceholderPatterns.ImagePlaceholderPattern,
                        Locations = kvp.Value.AsReadOnly(),
                        TotalOccurrences = kvp.Value.Sum(l => l.Occurrences),
                        Type = PlaceholderType.Image,
                        ImageProperties = new ImageProperties
                        {
                            ImageName = imageName,
                            MaxWidth = width,
                            MaxHeight = height
                        }
                    };
                }
            }
            
            // It's a text placeholder
            return new Placeholder
            {
                Name = kvp.Key,
                Pattern = pattern,
                Locations = kvp.Value.AsReadOnly(),
                TotalOccurrences = kvp.Value.Sum(l => l.Occurrences),
                Type = PlaceholderType.Text,
                ImageProperties = null
            };
        }).ToList();

        // Put SOUBOR_PREFIX first if it exists, keep the rest in original order
        var finalPlaceholders = new List<Placeholder>();
        var prefixPlaceholder = placeholderList.FirstOrDefault(p => string.Equals(p.Name, Placeholder.FilePrefixPlaceholder, StringComparison.OrdinalIgnoreCase));
        if (prefixPlaceholder != null)
        {
            finalPlaceholders.Add(prefixPlaceholder);
            finalPlaceholders.AddRange(placeholderList.Where(p => !string.Equals(p.Name, Placeholder.FilePrefixPlaceholder, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            finalPlaceholders.AddRange(placeholderList);
        }

        return new ScanFileResult
        {
            FilePath = templatePath,
            Placeholders = finalPlaceholders,
            Error = null
        };
    }

    private async Task ScanDocumentPartAsync(
        OpenXmlElement element,
        string filePath,
        string section,
        string pattern,
        Dictionary<string, List<PlaceholderLocation>> placeholders,
        CancellationToken cancellationToken)
    {
        // Use the new PlaceholderScanner for better detection of split placeholders
        var scanResults = await _placeholderScanner.ScanDocumentElementAsync(element, filePath, section, cancellationToken);
        
        // Merge the results into the existing placeholders dictionary
        foreach (var kvp in scanResults)
        {
            if (!placeholders.ContainsKey(kvp.Key))
            {
                placeholders[kvp.Key] = new List<PlaceholderLocation>();
            }
            
            foreach (var location in kvp.Value)
            {
                // Check if we already have this location
                var existingLocation = placeholders[kvp.Key].FirstOrDefault(l => 
                    l.FilePath == location.FilePath && l.Context == location.Context);
                
                if (existingLocation != null)
                {
                    // Update the occurrence count
                    var index = placeholders[kvp.Key].IndexOf(existingLocation);
                    placeholders[kvp.Key][index] = existingLocation with 
                    { 
                        Occurrences = existingLocation.Occurrences + location.Occurrences 
                    };
                }
                else
                {
                    placeholders[kvp.Key].Add(location);
                }
            }
        }
    }

    private class ScanFileResult
    {
        public required string FilePath { get; init; }
        public required IReadOnlyList<Placeholder> Placeholders { get; init; }
        public ScanError? Error { get; init; }
    }
}