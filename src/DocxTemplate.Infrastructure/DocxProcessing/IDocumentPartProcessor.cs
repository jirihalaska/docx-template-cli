using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Interface for processing individual parts of a Word document during traversal.
/// Implementations can scan for placeholders, perform replacements, or any other operations.
/// </summary>
public interface IDocumentPartProcessor
{
    /// <summary>
    /// Processes a specific part of a Word document.
    /// </summary>
    /// <param name="element">The OpenXML element to process (e.g., Body, Header, Footer).
    /// This contains all paragraphs, tables, and other content in this document part.</param>
    /// <param name="section">Human-readable label for the document part being processed 
    /// (e.g., "Body", "Header0", "Footer1"). Used for logging and providing context in results.</param>
    /// <param name="mainPart">The main document part containing references to all resources.
    /// Required for operations like adding images, accessing styles, or creating relationships.</param>
    /// <param name="cancellationToken">Token for cooperative cancellation of long-running operations.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task ProcessAsync(
        OpenXmlElement element, 
        string section, 
        MainDocumentPart mainPart, 
        CancellationToken cancellationToken);
}