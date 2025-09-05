using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Scanner for detecting placeholders in Word documents, handling text run splitting.
/// Now uses the unified PlaceholderReplacementEngine for scanning operations.
/// </summary>
public class PlaceholderScanner
{
    private readonly ILogger<PlaceholderScanner> _logger;
    private readonly PlaceholderReplacementEngine _replacementEngine;
    
    public PlaceholderScanner(ILogger<PlaceholderScanner> logger, PlaceholderReplacementEngine replacementEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _replacementEngine = replacementEngine ?? throw new ArgumentNullException(nameof(replacementEngine));
    }
    
    /// <summary>
    /// Scans a document element for both text and image placeholders
    /// </summary>
    public async Task<Dictionary<string, List<PlaceholderLocation>>> ScanDocumentElementAsync(
        OpenXmlElement element,
        string filePath,
        string section,
        CancellationToken cancellationToken = default)
    {
        var placeholders = new Dictionary<string, List<PlaceholderLocation>>();
        var processedParagraphs = new HashSet<Paragraph>();
        
        await Task.Run(() =>
        {
            // First scan tables as they have more specific context
            var tables = element.Descendants<Table>().ToList();
            foreach (var table in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var tableCells = table.Descendants<TableCell>().ToList();
                foreach (var cell in tableCells)
                {
                    var cellParagraphs = cell.Descendants<Paragraph>().ToList();
                    foreach (var paragraph in cellParagraphs)
                    {
                        processedParagraphs.Add(paragraph); // Mark as processed
                        
                        var paragraphResult = _replacementEngine.ProcessParagraph(paragraph, ProcessingMode.Scan);
                        if (paragraphResult.DiscoveredPlaceholders.Count == 0)
                            continue;
                        
                        ProcessDiscoveredPlaceholders(paragraphResult.DiscoveredPlaceholders, filePath, $"{section} (Table)", placeholders);
                    }
                }
            }
            
            // Process all paragraphs that are not in tables
            var paragraphs = element.Descendants<Paragraph>().ToList();
            
            foreach (var paragraph in paragraphs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Skip if already processed as part of a table
                if (processedParagraphs.Contains(paragraph))
                    continue;
                
                // Use the replacement engine to scan this paragraph
                var paragraphResult = _replacementEngine.ProcessParagraph(paragraph, ProcessingMode.Scan);
                
                if (paragraphResult.DiscoveredPlaceholders.Count == 0)
                    continue;
                
                ProcessDiscoveredPlaceholders(paragraphResult.DiscoveredPlaceholders, filePath, section, placeholders);
            }
        }, cancellationToken);
        
        return placeholders;
    }
    
    /// <summary>
    /// Processes discovered placeholders from the replacement engine and adds them to the results dictionary
    /// </summary>
    private void ProcessDiscoveredPlaceholders(
        List<DiscoveredPlaceholder> discoveredPlaceholders,
        string filePath,
        string section,
        Dictionary<string, List<PlaceholderLocation>> placeholders)
    {
        foreach (var discovered in discoveredPlaceholders)
        {
            var placeholderKey = discovered.Name;
                
            if (!placeholders.ContainsKey(placeholderKey))
            {
                placeholders[placeholderKey] = new List<PlaceholderLocation>();
            }
            
            // Check if we already have this location
            var existingLocation = placeholders[placeholderKey].FirstOrDefault(l => 
                l.FilePath == discovered.FilePath && l.Context?.Contains(section) == true);
            
            if (existingLocation != null)
            {
                // Increment the occurrence count
                var index = placeholders[placeholderKey].IndexOf(existingLocation);
                placeholders[placeholderKey][index] = existingLocation with 
                { 
                    Occurrences = existingLocation.Occurrences + 1 
                };
            }
            else
            {
                // Add new location
                var fileName = Path.GetFileName(discovered.FilePath);
                placeholders[placeholderKey].Add(new PlaceholderLocation
                {
                    FileName = fileName,
                    FilePath = discovered.FilePath,
                    Occurrences = 1,
                    Context = $"{section}: {discovered.Context}"
                });
            }
            
            var logMessage = discovered.Type == PlaceholderType.Image 
                ? $"Found image placeholder: {discovered.Name}"
                : $"Found text placeholder: {discovered.Name}";
            _logger.LogDebug("{Message} in {File}", logMessage, Path.GetFileName(discovered.FilePath));
        }
    }

    /// <summary>
    /// Reconstructs the full text of a paragraph by joining all text runs.
    /// This functionality is now handled internally by the PlaceholderReplacementEngine.
    /// This method is kept for backward compatibility but delegates to basic text joining.
    /// </summary>
    public string ReconstructParagraphText(Paragraph paragraph)
    {
        if (paragraph == null)
            return string.Empty;
        
        var runs = paragraph.Descendants<Run>().ToList();
        var textParts = new List<string>();
        
        foreach (var run in runs)
        {
            var texts = run.Descendants<Text>().ToList();
            foreach (var text in texts)
            {
                if (text.Text != null)
                {
                    textParts.Add(text.Text);
                }
            }
        }
        
        return string.Join("", textParts);
    }
}