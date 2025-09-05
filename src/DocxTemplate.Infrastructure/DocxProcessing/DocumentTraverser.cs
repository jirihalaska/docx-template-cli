using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Provides unified document traversal for Word documents, ensuring all parts
/// (body, headers, footers) are processed consistently.
/// </summary>
public class DocumentTraverser
{
    private readonly ILogger<DocumentTraverser> _logger;
    
    public DocumentTraverser(ILogger<DocumentTraverser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Traverses all parts of a Word document and applies the provided processor to each part.
    /// </summary>
    /// <param name="filePath">Path to the Word document to process.</param>
    /// <param name="isReadOnly">If true, opens document in read-only mode. If false, changes can be saved.</param>
    /// <param name="processor">The processor to apply to each document part.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    public async Task TraverseDocumentAsync(
        string filePath,
        bool isReadOnly,
        dynamic processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(processor);
        
        _logger.LogDebug("Opening document {FilePath} (ReadOnly: {IsReadOnly})", filePath, isReadOnly);
        
        using var document = WordprocessingDocument.Open(filePath, !isReadOnly);
        var mainDocumentPart = document.MainDocumentPart;
        
        if (mainDocumentPart == null)
        {
            throw new InvalidOperationException($"Document has no main document part: {filePath}");
        }
        
        // Process all headers first (documents can have multiple for odd/even pages, first page, etc.)
        if (mainDocumentPart.HeaderParts != null)
        {
            int headerIndex = 0;
            foreach (var headerPart in mainDocumentPart.HeaderParts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (headerPart.Header != null)
                {
                    var section = $"Header{headerIndex}";
                    _logger.LogDebug("Processing {Section}", section);
                    await processor.ProcessAsync(
                        headerPart.Header, 
                        section, 
                        headerPart, // Headers use their own HeaderPart for relationships
                        cancellationToken);
                }
                headerIndex++;
            }
        }
        
        // Process main document body
        if (mainDocumentPart.Document?.Body != null)
        {
            _logger.LogDebug("Processing document body");
            await processor.ProcessAsync(
                mainDocumentPart.Document.Body, 
                "Body", 
                mainDocumentPart, // Body uses MainDocumentPart
                cancellationToken);
        }
        
        // Process all footers
        if (mainDocumentPart.FooterParts != null)
        {
            int footerIndex = 0;
            foreach (var footerPart in mainDocumentPart.FooterParts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (footerPart.Footer != null)
                {
                    var section = $"Footer{footerIndex}";
                    _logger.LogDebug("Processing {Section}", section);
                    await processor.ProcessAsync(
                        footerPart.Footer, 
                        section, 
                        footerPart, // Footers use their own FooterPart for relationships
                        cancellationToken);
                }
                footerIndex++;
            }
        }
        
        // Save changes if document was opened for writing
        if (!isReadOnly)
        {
            _logger.LogDebug("Saving document changes");
            mainDocumentPart.Document?.Save();
        }
    }
    
    /// <summary>
    /// Gets all paragraphs from a document element, properly handling tables.
    /// Returns table paragraphs first (with table context), then non-table paragraphs.
    /// </summary>
    /// <param name="element">The document element to extract paragraphs from.</param>
    /// <returns>All paragraphs in the element, with table paragraphs first.</returns>
    public static IEnumerable<Paragraph> GetAllParagraphs(OpenXmlElement element)
    {
        var processedParagraphs = new HashSet<Paragraph>();
        
        // First process paragraphs within tables (they have more specific context)
        var tables = element.Descendants<Table>().ToList();
        foreach (var table in tables)
        {
            var tableParagraphs = table.Descendants<Paragraph>().ToList();
            foreach (var paragraph in tableParagraphs)
            {
                processedParagraphs.Add(paragraph);
                yield return paragraph;
            }
        }
        
        // Then process paragraphs not in tables
        var allParagraphs = element.Descendants<Paragraph>().ToList();
        foreach (var paragraph in allParagraphs)
        {
            if (!processedParagraphs.Contains(paragraph))
            {
                yield return paragraph;
            }
        }
    }
}