using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Scanner for detecting placeholders in Word documents, handling text run splitting
/// </summary>
public class PlaceholderScanner
{
    private readonly ILogger<PlaceholderScanner> _logger;
    private readonly PlaceholderProcessor _processor;
    
    public PlaceholderScanner(ILogger<PlaceholderScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create processor with its own logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _processor = new PlaceholderProcessor(loggerFactory.CreateLogger<PlaceholderProcessor>());
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
                        
                        var fullText = _processor.ReconstructParagraphText(paragraph);
                        if (string.IsNullOrWhiteSpace(fullText))
                            continue;
                        
                        var matches = _processor.FindAllPlaceholders(fullText, filePath, $"{section} (Table)");
                        ProcessPlaceholderMatches(matches, placeholders);
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
                
                // Use unified processor to reconstruct text and find placeholders
                var fullText = _processor.ReconstructParagraphText(paragraph);
                
                if (string.IsNullOrWhiteSpace(fullText))
                    continue;
                
                // Use unified processor to find all placeholders
                var matches = _processor.FindAllPlaceholders(fullText, filePath, section);
                ProcessPlaceholderMatches(matches, placeholders);
            }
        }, cancellationToken);
        
        return placeholders;
    }
    
    /// <summary>
    /// Processes placeholder matches found by the unified processor and adds them to the results dictionary
    /// </summary>
    private void ProcessPlaceholderMatches(
        List<PlaceholderMatch> matches,
        Dictionary<string, List<PlaceholderLocation>> placeholders)
    {
        foreach (var match in matches)
        {
            if (!placeholders.ContainsKey(match.PlaceholderKey))
            {
                placeholders[match.PlaceholderKey] = new List<PlaceholderLocation>();
            }
            
            // Check if we already have this location
            var existingLocation = placeholders[match.PlaceholderKey].FirstOrDefault(l => 
                l.FilePath == match.FilePath && l.Context?.Contains(match.Section) == true);
            
            if (existingLocation != null)
            {
                // Increment the occurrence count
                var index = placeholders[match.PlaceholderKey].IndexOf(existingLocation);
                placeholders[match.PlaceholderKey][index] = existingLocation with 
                { 
                    Occurrences = existingLocation.Occurrences + 1 
                };
            }
            else
            {
                // Add new location
                var fileName = Path.GetFileName(match.FilePath);
                placeholders[match.PlaceholderKey].Add(new PlaceholderLocation
                {
                    FileName = fileName,
                    FilePath = match.FilePath,
                    Occurrences = 1,
                    Context = $"{match.Section}: {match.Context}"
                });
            }
            
            var logMessage = match.Type == PlaceholderType.Image 
                ? $"Found image placeholder: {match.PlaceholderName}"
                : $"Found text placeholder: {match.PlaceholderName}";
            _logger.LogDebug("{Message} in {File}", logMessage, Path.GetFileName(match.FilePath));
        }
    }

    /// <summary>
    /// Reconstructs the full text of a paragraph by joining all text runs.
    /// Delegates to the unified processor for consistency.
    /// </summary>
    public string ReconstructParagraphText(Paragraph paragraph)
    {
        return _processor.ReconstructParagraphText(paragraph);
    }
}