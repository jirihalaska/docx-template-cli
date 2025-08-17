using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for scanning DOCX templates to discover placeholder patterns using OpenXML
/// </summary>
public class PlaceholderScanService : IPlaceholderScanService
{
    private readonly ITemplateDiscoveryService _discoveryService;
    private readonly ILogger<PlaceholderScanService> _logger;

    public PlaceholderScanService(
        ITemplateDiscoveryService discoveryService,
        ILogger<PlaceholderScanService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                Array.Empty<Placeholder>(),
                0,
                duration,
                0,
                0,
                new[] { new ScanError { FilePath = folderPath, Message = ex.Message, ExceptionType = ex.GetType().Name } });
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
                    Placeholders = Array.Empty<Placeholder>(),
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
                        allPlaceholders[placeholder.Name] = new List<PlaceholderLocation>();
                    }

                    allPlaceholders[placeholder.Name].AddRange(placeholder.Locations);
                }
            }
        }

        // Create final placeholder objects with aggregated locations
        var finalPlaceholders = allPlaceholders.Select(kvp => new Placeholder
        {
            Name = kvp.Key,
            Pattern = pattern,
            Locations = kvp.Value.AsReadOnly(),
            TotalOccurrences = kvp.Value.Sum(l => l.Occurrences)
        }).ToList();

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
            return Array.Empty<string>();

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
                Placeholders = Array.Empty<Placeholder>(),
                Error = new ScanError
                {
                    FilePath = templatePath,
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name
                }
            };
        }

        // Convert to final placeholder objects
        var finalPlaceholders = placeholders.Select(kvp => new Placeholder
        {
            Name = kvp.Key,
            Pattern = pattern,
            Locations = kvp.Value.AsReadOnly(),
            TotalOccurrences = kvp.Value.Sum(l => l.Occurrences)
        }).ToList();

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
        await Task.Run(() =>
        {
            // Get all text content from the element, handling split runs
            var textContent = GetTextContent(element);
            if (string.IsNullOrEmpty(textContent))
                return;

            // Extract placeholder names from the text
            var placeholderNames = ExtractPlaceholderNames(textContent, pattern);

            foreach (var name in placeholderNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!placeholders.ContainsKey(name))
                {
                    placeholders[name] = new List<PlaceholderLocation>();
                }

                // Count occurrences of this placeholder in the text
                var regex = new Regex(pattern.Replace(".*?", Regex.Escape(name)), RegexOptions.Compiled, TimeSpan.FromSeconds(1));
                var matches = regex.Matches(textContent);

                if (matches.Count > 0)
                {
                    var fileName = Path.GetFileName(filePath);
                    placeholders[name].Add(new PlaceholderLocation
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        Occurrences = matches.Count,
                        Context = $"{section}: {GetContextAroundPlaceholder(textContent, name, 50)}"
                    });
                }
            }
        }, cancellationToken);
    }

    private string GetTextContent(OpenXmlElement element)
    {
        var texts = new List<string>();
        
        // Recursively collect all text content, handling text runs
        CollectTextRecursive(element, texts);
        
        return string.Join(" ", texts.Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    private void CollectTextRecursive(OpenXmlElement element, List<string> texts)
    {
        if (element is Text textElement)
        {
            texts.Add(textElement.Text);
        }
        else
        {
            foreach (var child in element.Elements())
            {
                CollectTextRecursive(child, texts);
            }
        }
    }

    private string GetContextAroundPlaceholder(string text, string placeholderName, int contextLength)
    {
        var index = text.IndexOf(placeholderName, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
            return string.Empty;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + placeholderName.Length + contextLength);
        
        var context = text.Substring(start, end - start);
        
        // Add ellipsis if we truncated
        if (start > 0)
            context = "..." + context;
        if (end < text.Length)
            context = context + "...";

        return context.Trim();
    }

    private class ScanFileResult
    {
        public required string FilePath { get; init; }
        public required IReadOnlyList<Placeholder> Placeholders { get; init; }
        public ScanError? Error { get; init; }
    }
}