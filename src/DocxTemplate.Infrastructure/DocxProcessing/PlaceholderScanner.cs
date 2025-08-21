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
    
    public PlaceholderScanner(ILogger<PlaceholderScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                        
                        var fullText = ReconstructParagraphText(paragraph);
                        if (string.IsNullOrWhiteSpace(fullText))
                            continue;
                        
                        ScanForImagePlaceholders(fullText, filePath, $"{section} (Table)", placeholders);
                        ScanForTextPlaceholders(fullText, filePath, $"{section} (Table)", placeholders);
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
                
                // Reconstruct the full paragraph text to handle split placeholders
                var fullText = ReconstructParagraphText(paragraph);
                
                if (string.IsNullOrWhiteSpace(fullText))
                    continue;
                
                // Scan for both image and text placeholders
                ScanForImagePlaceholders(fullText, filePath, section, placeholders);
                ScanForTextPlaceholders(fullText, filePath, section, placeholders);
            }
        }, cancellationToken);
        
        return placeholders;
    }
    
    /// <summary>
    /// Reconstructs the full text of a paragraph by joining all text runs
    /// </summary>
    /// <remarks>
    /// Critical: Word splits placeholders across multiple runs. This method reconstructs
    /// the complete text to detect placeholders that span multiple runs.
    /// </remarks>
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
    
    /// <summary>
    /// Scans text for image placeholders
    /// </summary>
    private void ScanForImagePlaceholders(
        string text,
        string filePath,
        string section,
        Dictionary<string, List<PlaceholderLocation>> placeholders)
    {
        var regex = new Regex(PlaceholderPatterns.ImagePlaceholderPattern, RegexOptions.Compiled);
        var matches = regex.Matches(text);
        
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;
            
            var imageName = match.Groups[1].Value;
            var width = match.Groups[2].Value;
            var height = match.Groups[3].Value;
            
            // Create a key that includes the dimensions for uniqueness
            var key = $"image:{imageName}|width:{width}|height:{height}";
            
            if (!placeholders.ContainsKey(key))
            {
                placeholders[key] = new List<PlaceholderLocation>();
            }
            
            // Check if we already have this location
            var existingLocation = placeholders[key].FirstOrDefault(l => 
                l.FilePath == filePath && l.Context?.Contains(section) == true);
            
            if (existingLocation != null)
            {
                // Increment the occurrence count
                var index = placeholders[key].IndexOf(existingLocation);
                placeholders[key][index] = existingLocation with 
                { 
                    Occurrences = existingLocation.Occurrences + 1 
                };
            }
            else
            {
                // Add new location
                var fileName = Path.GetFileName(filePath);
                placeholders[key].Add(new PlaceholderLocation
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Occurrences = 1,
                    Context = $"{section}: {GetContextAroundPlaceholder(text, match.Value, 50)}"
                });
            }
            
            _logger.LogDebug("Found image placeholder: {Name} with dimensions {Width}x{Height} in {File}",
                imageName, width, height, Path.GetFileName(filePath));
        }
    }
    
    /// <summary>
    /// Scans text for text placeholders (excluding image placeholders)
    /// </summary>
    private void ScanForTextPlaceholders(
        string text,
        string filePath,
        string section,
        Dictionary<string, List<PlaceholderLocation>> placeholders)
    {
        // First, remove all image placeholders from the text to avoid double detection
        var textWithoutImages = Regex.Replace(text, PlaceholderPatterns.ImagePlaceholderPattern, "");
        
        // Now scan for text placeholders
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern, RegexOptions.Compiled);
        var matches = regex.Matches(textWithoutImages);
        
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;
            
            // Extract the placeholder name
            var name = match.Groups[1].Value.Trim();
            
            if (string.IsNullOrWhiteSpace(name))
                continue;
            
            if (!placeholders.ContainsKey(name))
            {
                placeholders[name] = new List<PlaceholderLocation>();
            }
            
            // Check if we already have this location
            var existingLocation = placeholders[name].FirstOrDefault(l => 
                l.FilePath == filePath && l.Context?.Contains(section) == true);
            
            if (existingLocation != null)
            {
                // Increment the occurrence count
                var index = placeholders[name].IndexOf(existingLocation);
                placeholders[name][index] = existingLocation with 
                { 
                    Occurrences = existingLocation.Occurrences + 1 
                };
            }
            else
            {
                // Add new location
                var fileName = Path.GetFileName(filePath);
                placeholders[name].Add(new PlaceholderLocation
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Occurrences = 1,
                    Context = $"{section}: {GetContextAroundPlaceholder(text, match.Value, 50)}"
                });
            }
            
            _logger.LogDebug("Found text placeholder: {Name} in {File}",
                name, Path.GetFileName(filePath));
        }
    }
    
    private string GetContextAroundPlaceholder(string text, string placeholderValue, int contextLength)
    {
        var index = text.IndexOf(placeholderValue, StringComparison.Ordinal);
        if (index == -1)
            return string.Empty;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + placeholderValue.Length + contextLength);
        
        var context = text.Substring(start, end - start);
        
        // Add ellipsis if we truncated
        if (start > 0)
            context = "..." + context;
        if (end < text.Length)
            context = context + "...";

        return context.Trim();
    }
}