using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Document processor implementation for replacing placeholders in document parts.
/// </summary>
internal class ReplaceDocumentPartProcessor : IDocumentPartProcessor
{
    private readonly ReplacementMap _replacementMap;
    private readonly ILogger<ReplaceDocumentPartProcessor> _logger;
    private int _totalReplacements;
    
    // Method reference from PlaceholderReplaceService for processing individual paragraphs
    private readonly Func<Paragraph, ReplacementMap, OpenXmlPart, int> _processReplacements;
    
    public ReplaceDocumentPartProcessor(
        ReplacementMap replacementMap,
        Func<Paragraph, ReplacementMap, OpenXmlPart, int> processReplacements,
        ILogger<ReplaceDocumentPartProcessor> logger)
    {
        _replacementMap = replacementMap ?? throw new ArgumentNullException(nameof(replacementMap));
        _processReplacements = processReplacements ?? throw new ArgumentNullException(nameof(processReplacements));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Gets the total number of replacements made across all document parts.
    /// </summary>
    public int TotalReplacements => _totalReplacements;
    
    public async Task ProcessAsync(
        OpenXmlElement element,
        string section,
        OpenXmlPart documentPart,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing replacements in {Section}", section);
        
        var sectionReplacements = 0;
        
        // Get all paragraphs using the helper method from DocumentTraverser
        var paragraphs = DocumentTraverser.GetAllParagraphs(element);
        
        foreach (var paragraph in paragraphs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            // Use existing replacement logic from PlaceholderReplaceService
            // This handles both text and image placeholders with alignment preservation
            // Now uses the correct document part (HeaderPart for headers, etc.)
            sectionReplacements += _processReplacements(paragraph, _replacementMap, documentPart);
        }
        
        _totalReplacements += sectionReplacements;
        _logger.LogDebug("Replaced {Count} placeholders in {Section}", 
            sectionReplacements, section);
        
        await Task.CompletedTask;
    }
}