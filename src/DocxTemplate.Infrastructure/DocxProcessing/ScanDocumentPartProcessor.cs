using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocxTemplate.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Document processor implementation for scanning placeholders in document parts.
/// </summary>
internal class ScanDocumentPartProcessor : IDocumentPartProcessor
{
    private readonly PlaceholderScanner _scanner;
    private readonly string _filePath;
    private readonly Dictionary<string, List<PlaceholderLocation>> _placeholders;
    private readonly ILogger<ScanDocumentPartProcessor> _logger;
    
    public ScanDocumentPartProcessor(
        PlaceholderScanner scanner,
        string filePath,
        Dictionary<string, List<PlaceholderLocation>> placeholders,
        ILogger<ScanDocumentPartProcessor> logger)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _placeholders = placeholders ?? throw new ArgumentNullException(nameof(placeholders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task ProcessAsync(
        OpenXmlElement element,
        string section,
        MainDocumentPart mainPart,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scanning {Section} for placeholders", section);
        
        // Use existing PlaceholderScanner to scan this element
        var scanResults = await _scanner.ScanDocumentElementAsync(
            element, 
            _filePath, 
            section, 
            cancellationToken);
        
        // Merge results into the main placeholders dictionary
        foreach (var kvp in scanResults)
        {
            if (!_placeholders.ContainsKey(kvp.Key))
            {
                _placeholders[kvp.Key] = new List<PlaceholderLocation>();
            }
            
            foreach (var location in kvp.Value)
            {
                // Check if we already have this location to avoid duplicates
                var existingLocation = _placeholders[kvp.Key].FirstOrDefault(l => 
                    l.FilePath == location.FilePath && l.Context == location.Context);
                
                if (existingLocation != null)
                {
                    // Update the occurrence count
                    var index = _placeholders[kvp.Key].IndexOf(existingLocation);
                    _placeholders[kvp.Key][index] = existingLocation with 
                    { 
                        Occurrences = existingLocation.Occurrences + location.Occurrences 
                    };
                }
                else
                {
                    _placeholders[kvp.Key].Add(location);
                }
            }
        }
        
        _logger.LogDebug("Found {Count} unique placeholders in {Section}", 
            scanResults.Count, section);
    }
}