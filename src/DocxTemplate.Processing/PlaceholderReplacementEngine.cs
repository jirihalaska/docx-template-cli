using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocxTemplate.Processing.Models;
using DocxTemplate.Processing.Interfaces;
using DocxTemplate.Processing.Images;
using Microsoft.Extensions.Logging;

using W = DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

namespace DocxTemplate.Processing;

/// <summary>
/// Unified engine for processing placeholder replacement in Word documents.
/// Consolidates all placeholder discovery, text reconstruction, and replacement logic.
/// </summary>
public class PlaceholderReplacementEngine
{
    private readonly ILogger<PlaceholderReplacementEngine> _logger;
    private readonly IImageProcessor _imageProcessor;

    private static readonly Regex TextPlaceholderPattern = new(PlaceholderPatterns.TextPlaceholderPattern, RegexOptions.Compiled);
    private static readonly Regex ImagePlaceholderPattern = new(PlaceholderPatterns.ImagePlaceholderPattern, RegexOptions.Compiled);

    public PlaceholderReplacementEngine(
        ILogger<PlaceholderReplacementEngine> logger,
        IImageProcessor imageProcessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
    }

    /// <summary>
    /// Processes an entire document for placeholder operations.
    /// </summary>
    public async Task<DocumentProcessingResult> ProcessDocumentAsync(
        string filePath,
        ProcessingMode mode,
        ReplacementMap? replacementMap = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (mode == ProcessingMode.Replace)
        {
            ArgumentNullException.ThrowIfNull(replacementMap);
        }

        _logger.LogDebug("Processing document {FilePath} in {Mode} mode", filePath, mode);

        using var document = WordprocessingDocument.Open(filePath, mode == ProcessingMode.Replace);
        var mainDocumentPart = document.MainDocumentPart;

        if (mainDocumentPart == null)
        {
            throw new InvalidOperationException($"Document has no main document part: {filePath}");
        }

        var result = new DocumentProcessingResult();

        // Process all headers first
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

                    var sectionResult = await ProcessDocumentPartAsync(
                        headerPart.Header,
                        section,
                        headerPart,
                        mode,
                        replacementMap,
                        cancellationToken);

                    result.SectionResults.Add(sectionResult);
                }
                headerIndex++;
            }
        }

        // Process main document body
        if (mainDocumentPart.Document?.Body != null)
        {
            _logger.LogDebug("Processing document body");
            var sectionResult = await ProcessDocumentPartAsync(
                mainDocumentPart.Document.Body,
                "Body",
                mainDocumentPart,
                mode,
                replacementMap,
                cancellationToken);

            result.SectionResults.Add(sectionResult);
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

                    var sectionResult = await ProcessDocumentPartAsync(
                        footerPart.Footer,
                        section,
                        footerPart,
                        mode,
                        replacementMap,
                        cancellationToken);

                    result.SectionResults.Add(sectionResult);
                }
                footerIndex++;
            }
        }

        // Aggregate results
        result.DiscoveredPlaceholders = result.SectionResults
            .SelectMany(s => s.DiscoveredPlaceholders)
            .ToList();

        result.ReplacementsPerformed = result.SectionResults
            .Sum(s => s.ReplacementsPerformed);

        // Aggregate detailed replacements from all sections
        foreach (var section in result.SectionResults)
        {
            foreach (var detailKvp in section.DetailedReplacements)
            {
                if (result.DetailedReplacements.ContainsKey(detailKvp.Key))
                {
                    result.DetailedReplacements[detailKvp.Key] += detailKvp.Value;
                }
                else
                {
                    result.DetailedReplacements[detailKvp.Key] = detailKvp.Value;
                }
            }
        }

        _logger.LogDebug("Completed processing document {FilePath}. Found {PlaceholderCount} placeholders, performed {ReplacementCount} replacements",
            filePath, result.DiscoveredPlaceholders.Count, result.ReplacementsPerformed);

        return result;
    }

    /// <summary>
    /// Processes a single paragraph for placeholder operations.
    /// </summary>
    public ParagraphProcessingResult ProcessParagraph(
        W.Paragraph paragraph,
        ProcessingMode mode,
        ReplacementMap? replacementMap = null,
        OpenXmlPart? documentPart = null)
    {
        ArgumentNullException.ThrowIfNull(paragraph);

        if (mode == ProcessingMode.Replace)
        {
            ArgumentNullException.ThrowIfNull(replacementMap);
        }

        var result = new ParagraphProcessingResult();

        // Step 1: Reconstruct full paragraph text
        var fullText = ReconstructParagraphText(paragraph);
        if (string.IsNullOrWhiteSpace(fullText))
        {
            return result;
        }

        // Step 2: Find all placeholders
        var allMatches = FindAllPlaceholders(fullText, documentPart?.Uri?.ToString() ?? "unknown", "paragraph");
        result.DiscoveredPlaceholders = allMatches.Select(m => new DiscoveredPlaceholder
        {
            Name = m.Type == PlaceholderType.Image ? m.PlaceholderKey : m.PlaceholderName,
            Type = m.Type,
            Context = m.Context,
            FilePath = m.FilePath,
        }).ToList();

        if (mode == ProcessingMode.Scan)
        {
            return result;
        }

        // Step 3: Process replacements
        var imageMatches = allMatches.Where(m => m.Type == PlaceholderType.Image).ToList();
        var textMatches = allMatches.Where(m => m.Type == PlaceholderType.Text).ToList();

        // Process image placeholders first (they replace the entire paragraph)
        if (imageMatches.Count > 0)
        {
            foreach (var match in imageMatches)
            {
                if (TryReplaceImagePlaceholder(paragraph, match, replacementMap!, documentPart))
                {
                    result.ReplacementsPerformed++;
                    
                    // Track detailed image replacement
                    if (replacementMap.Mappings.TryGetValue(match.PlaceholderName, out var imagePath))
                    {
                        var placeholderKey = $"{match.PlaceholderName}→{imagePath}";
                        if (result.DetailedReplacements.ContainsKey(placeholderKey))
                        {
                            result.DetailedReplacements[placeholderKey]++;
                        }
                        else
                        {
                            result.DetailedReplacements[placeholderKey] = 1;
                        }
                    }
                }
            }
            return result;
        }

        // Process text placeholders using right-to-left strategy
        if (textMatches.Count > 0)
        {
            result.ReplacementsPerformed = ReplaceTextPlaceholders(paragraph, textMatches, replacementMap!, documentPart, result.DetailedReplacements);
        }

        return result;
    }

    private Task<SectionProcessingResult> ProcessDocumentPartAsync(
        OpenXmlElement element,
        string section,
        OpenXmlPart documentPart,
        ProcessingMode mode,
        ReplacementMap? replacementMap,
        CancellationToken cancellationToken)
    {
        var result = new SectionProcessingResult();
        var paragraphs = DocumentTraverser.GetAllParagraphs(element);

        foreach (var paragraph in paragraphs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var paragraphResult = ProcessParagraph(paragraph, mode, replacementMap, documentPart);
            result.DiscoveredPlaceholders.AddRange(paragraphResult.DiscoveredPlaceholders);
            result.ReplacementsPerformed += paragraphResult.ReplacementsPerformed;
            
            // Aggregate detailed replacements
            foreach (var detailKvp in paragraphResult.DetailedReplacements)
            {
                if (result.DetailedReplacements.ContainsKey(detailKvp.Key))
                {
                    result.DetailedReplacements[detailKvp.Key] += detailKvp.Value;
                }
                else
                {
                    result.DetailedReplacements[detailKvp.Key] = detailKvp.Value;
                }
            }
        }

        _logger.LogDebug("Processed {Section}: found {PlaceholderCount} placeholders, performed {ReplacementCount} replacements",
            section, result.DiscoveredPlaceholders.Count, result.ReplacementsPerformed);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Reconstructs the full text of a paragraph by joining all text runs.
    /// This is critical for detecting placeholders that are split across multiple runs.
    /// </summary>
    private string ReconstructParagraphText(W.Paragraph paragraph)
    {
        if (paragraph == null)
            return string.Empty;

        var runs = paragraph.Descendants<W.Run>().ToList();
        var textParts = new List<string>();

        foreach (var run in runs)
        {
            var texts = run.Descendants<W.Text>().ToList();
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
    /// Builds a detailed map of text elements with their positions in the full paragraph text.
    /// This is used for both scanning (to determine context) and replacement (to update correct elements).
    /// </summary>
    private List<TextElementInfo> BuildTextElementMap(W.Paragraph paragraph)
    {
        var textElements = new List<TextElementInfo>();
        var runs = paragraph.Descendants<W.Run>().ToList();
        var currentPos = 0;

        foreach (var run in runs)
        {
            var texts = run.Descendants<W.Text>().ToList();
            foreach (var text in texts)
            {
                var textLength = text.Text?.Length ?? 0;
                textElements.Add(new TextElementInfo
                {
                    TextElement = text,
                    StartPosition = currentPos,
                    EndPosition = currentPos + textLength,
                    OriginalText = text.Text ?? string.Empty
                });
                currentPos += textLength;
            }
        }

        return textElements;
    }

    /// <summary>
    /// Finds all placeholders in the given text and returns detailed information about each match.
    /// </summary>
    private List<PlaceholderMatch> FindAllPlaceholders(string fullText, string filePath, string section)
    {
        var matches = new List<PlaceholderMatch>();

        // Find image placeholders first (they take precedence)
        var imageMatches = ImagePlaceholderPattern.Matches(fullText);
        foreach (Match match in imageMatches)
        {
            if (match.Success)
            {
                var imageName = match.Groups[1].Value;
                var width = match.Groups[2].Value;
                var height = match.Groups[3].Value;
                var key = $"image:{imageName}|width:{width}|height:{height}";

                matches.Add(new PlaceholderMatch
                {
                    PlaceholderName = imageName,
                    PlaceholderKey = key,
                    FullMatch = match.Value,
                    StartIndex = match.Index,
                    Length = match.Length,
                    Type = PlaceholderType.Image,
                    FilePath = filePath,
                    Section = section,
                    Context = GetContextAroundPlaceholder(fullText, match.Value, 50),
                    Width = int.Parse(width, CultureInfo.InvariantCulture),
                    Height = int.Parse(height, CultureInfo.InvariantCulture)
                });
            }
        }

        // Find text placeholders, excluding areas already covered by image placeholders
        var textWithoutImages = ImagePlaceholderPattern.Replace(fullText, "");
        var textMatches = TextPlaceholderPattern.Matches(textWithoutImages);

        // Adjust positions back to original text
        var imageOffsets = CalculateImageOffsets(fullText);

        foreach (Match match in textMatches)
        {
            if (match.Success)
            {
                var name = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var adjustedIndex = AdjustPositionForImageRemovals(match.Index, imageOffsets);

                    matches.Add(new PlaceholderMatch
                    {
                        PlaceholderName = name,
                        PlaceholderKey = name,
                        FullMatch = match.Value,
                        StartIndex = adjustedIndex,
                        Length = match.Length,
                        Type = PlaceholderType.Text,
                        FilePath = filePath,
                        Section = section,
                        Context = GetContextAroundPlaceholder(fullText, match.Value, 50)
                    });
                }
            }
        }

        return matches.OrderBy(m => m.StartIndex).ToList();
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

    private List<ImageOffset> CalculateImageOffsets(string originalText)
    {
        var offsets = new List<ImageOffset>();
        var imageMatches = ImagePlaceholderPattern.Matches(originalText);

        foreach (Match match in imageMatches)
        {
            offsets.Add(new ImageOffset
            {
                StartIndex = match.Index,
                Length = match.Length
            });
        }

        return offsets.OrderBy(o => o.StartIndex).ToList();
    }

    private int AdjustPositionForImageRemovals(int position, List<ImageOffset> imageOffsets)
    {
        var adjustment = 0;
        foreach (var offset in imageOffsets)
        {
            if (offset.StartIndex < position)
            {
                adjustment += offset.Length;
            }
            else
            {
                break;
            }
        }
        return position + adjustment;
    }

    /// <summary>
    /// Replaces text placeholders using the proven right-to-left strategy with text map rebuilding.
    /// This avoids the index shifting problem when multiple placeholders are in the same paragraph.
    /// </summary>
    private int ReplaceTextPlaceholders(
        W.Paragraph paragraph,
        List<PlaceholderMatch> textMatches,
        ReplacementMap replacementMap,
        OpenXmlPart? documentPart,
        Dictionary<string, int> detailedReplacements)
    {
        var replacementCount = 0;

        // CRITICAL: Process placeholders in reverse order to maintain text positions
        var sortedMatches = textMatches.OrderByDescending(m => m.StartIndex).ToList();

        foreach (var match in sortedMatches)
        {
            if (!replacementMap.Mappings.TryGetValue(match.PlaceholderName, out var replacement))
                continue;

            // IMPORTANT: Rebuild text element map for each replacement
            // This is necessary because each replacement changes the document structure
            // and invalidates the position mapping for subsequent replacements
            var textElements = BuildTextElementMap(paragraph);

            // Re-find the placeholder in the current document state
            var currentFullText = ReconstructParagraphText(paragraph);
            var currentMatches = FindAllPlaceholders(currentFullText, documentPart?.Uri?.ToString() ?? "document", "paragraph")
                .Where(m => m.Type == PlaceholderType.Text &&
                           string.Equals(m.PlaceholderName, match.PlaceholderName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (currentMatches.Count > 0)
            {
                // Use the first match (there should typically be only one at this point)
                var currentMatch = currentMatches.First();

                // Use unified processor for replacement across elements
                if (ReplaceTextAcrossElements(textElements, currentMatch.StartIndex, currentMatch.Length, replacement))
                {
                    replacementCount++;
                    _logger.LogDebug("Replaced placeholder {PlaceholderName} with '{Replacement}'", match.PlaceholderName, replacement);
                    
                    // Track detailed replacement
                    var placeholderKey = $"{match.PlaceholderName}→{replacement}";
                    if (detailedReplacements.ContainsKey(placeholderKey))
                    {
                        detailedReplacements[placeholderKey]++;
                    }
                    else
                    {
                        detailedReplacements[placeholderKey] = 1;
                    }
                }
            }
        }

        // Fallback: Process individual text elements for placeholders that might not be detected
        // due to complex formatting within the placeholder text
        if (replacementCount == 0 && textMatches.Count > 0)
        {
            replacementCount += ProcessFallbackTextReplacement(paragraph, replacementMap, detailedReplacements);
        }

        return replacementCount;
    }

    /// <summary>
    /// Replaces a placeholder across multiple text elements by coordinating the replacement
    /// across all affected elements.
    /// </summary>
    private bool ReplaceTextAcrossElements(
        List<TextElementInfo> textElements,
        int startIndex,
        int length,
        string replacement)
    {
        var endIndex = startIndex + length;
        var affectedElements = textElements.Where(te =>
            te.StartPosition < endIndex && te.EndPosition > startIndex).ToList();

        if (affectedElements.Count == 0)
        {
            _logger.LogWarning("No text elements found for placeholder at position {StartIndex}-{EndIndex}. " +
                "Total elements: {ElementCount}, Text reconstruction: '{FullText}'",
                startIndex, endIndex, textElements.Count,
                string.Join("", textElements.Select(te => te.OriginalText)));
            return false;
        }

        _logger.LogDebug("Replacing placeholder at position {StartIndex}-{EndIndex} with '{Replacement}' " +
            "across {ElementCount} text elements", startIndex, endIndex, replacement, affectedElements.Count);

        try
        {
            // Validate that our text element map is consistent with the expected replacement
            var reconstructedText = string.Join("", textElements.Select(te => te.OriginalText));
            if (startIndex + length > reconstructedText.Length)
            {
                _logger.LogWarning("Placeholder position {StartIndex}-{EndIndex} exceeds reconstructed text length {TextLength}. " +
                    "This suggests a text element mapping inconsistency. Text: '{Text}'",
                    startIndex, endIndex, reconstructedText.Length, reconstructedText);

                // Try to repair the situation by rebuilding the text element positions
                return TryRepairAndReplaceText(textElements, startIndex, length, replacement);
            }

            // Process affected text elements
            for (int i = 0; i < affectedElements.Count; i++)
            {
                var element = affectedElements[i];

                var textStart = Math.Max(0, startIndex - element.StartPosition);
                var textEnd = Math.Min(element.OriginalText.Length, endIndex - element.StartPosition);

                // Additional validation
                if (textEnd > element.OriginalText.Length || textStart > textEnd)
                {
                    _logger.LogWarning("Invalid text bounds for element {ElementIndex}: textStart={TextStart}, " +
                        "textEnd={TextEnd}, originalLength={OriginalLength}, element='{ElementText}'",
                        i, textStart, textEnd, element.OriginalText.Length, element.OriginalText);
                    continue;
                }

                string newText;

                if (i == 0 && affectedElements.Count == 1)
                {
                    // Single element case - replace within the element
                    newText = element.OriginalText.Substring(0, textStart) +
                             replacement +
                             element.OriginalText.Substring(textEnd);
                }
                else if (i == 0)
                {
                    // First element - keep text before placeholder, add replacement
                    newText = element.OriginalText.Substring(0, textStart) + replacement;
                }
                else if (i == affectedElements.Count - 1)
                {
                    // Last element - keep text after placeholder
                    newText = element.OriginalText.Substring(textEnd);
                }
                else
                {
                    // Middle element - remove all text (it's part of the placeholder)
                    newText = string.Empty;
                }

                _logger.LogDebug("Element {ElementIndex}: '{OriginalText}' -> '{NewText}'",
                    i, element.OriginalText, newText);

                element.TextElement.Text = newText;

                // Remove empty text elements to clean up the document
                if (string.IsNullOrEmpty(newText))
                {
                    element.TextElement.Remove();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace text across elements for placeholder at position {StartIndex}", startIndex);
            return false;
        }
    }

    /// <summary>
    /// Attempts to repair text element mapping inconsistencies and perform replacement
    /// using a more robust approach.
    /// </summary>
    private bool TryRepairAndReplaceText(List<TextElementInfo> textElements, int startIndex, int length, string replacement)
    {
        _logger.LogDebug("Attempting to repair text element mapping and replace text");

        try
        {
            // Rebuild text element positions from scratch
            var currentPos = 0;
            foreach (var element in textElements)
            {
                var actualText = element.TextElement.Text ?? string.Empty;
                element.StartPosition = currentPos;
                element.EndPosition = currentPos + actualText.Length;
                element.OriginalText = actualText;
                currentPos += actualText.Length;
            }

            // Try replacement again with corrected positions
            return ReplaceTextAcrossElements(textElements, startIndex, length, replacement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed during repair attempt");
            return false;
        }
    }

    /// <summary>
    /// Fallback replacement approach that searches for placeholder patterns directly in text elements
    /// </summary>
    private int ProcessFallbackTextReplacement(W.Paragraph paragraph, ReplacementMap replacementMap, Dictionary<string, int> detailedReplacements)
    {
        var replacementCount = 0;
        var runs = paragraph.Descendants<W.Run>().ToList();

        foreach (var run in runs)
        {
            var textElements = run.Descendants<W.Text>().ToList();
            foreach (var textElement in textElements)
            {
                var originalText = textElement.Text;
                var newText = TextPlaceholderPattern.Replace(originalText, match =>
                {
                    var placeholderName = match.Groups[1].Value.Trim();
                    if (replacementMap.Mappings.TryGetValue(placeholderName, out var replacement))
                    {
                        replacementCount++;
                        _logger.LogDebug("Fallback replacement: {PlaceholderName} -> '{Replacement}'", placeholderName, replacement);
                        
                        // Track detailed replacement
                        var placeholderKey = $"{placeholderName}→{replacement}";
                        if (detailedReplacements.ContainsKey(placeholderKey))
                        {
                            detailedReplacements[placeholderKey]++;
                        }
                        else
                        {
                            detailedReplacements[placeholderKey] = 1;
                        }
                        
                        return replacement;
                    }
                    return match.Value; // Keep original if no replacement found
                });

                if (newText != originalText)
                {
                    textElement.Text = newText;
                }
            }
        }

        return replacementCount;
    }

    /// <summary>
    /// Replaces an image placeholder with the actual image file.
    /// </summary>
    private bool TryReplaceImagePlaceholder(
        W.Paragraph paragraph,
        PlaceholderMatch match,
        ReplacementMap replacementMap,
        OpenXmlPart? documentPart)
    {
        try
        {
            var imageName = match.PlaceholderName;
            var width = match.Width ?? 100;
            var height = match.Height ?? 100;

            // Check if we have a mapping for this image placeholder
            if (!replacementMap.Mappings.TryGetValue(imageName, out var imagePath) ||
                string.IsNullOrWhiteSpace(imagePath) ||
                !File.Exists(imagePath))
            {
                _logger.LogWarning("Image file not found for placeholder {ImageName}: {ImagePath}", imageName, imagePath);
                return false;
            }

            // Get image information
            var imageInfo = _imageProcessor.GetImageInfo(imagePath);

            // Calculate display dimensions while preserving aspect ratio
            var (displayWidth, displayHeight) = AspectRatioCalculator.CalculateDisplayDimensions(
                imageInfo.Width, imageInfo.Height, width, height);

            // Convert to EMUs
            var widthEmus = UnitConverter.PixelsToEmus(displayWidth);
            var heightEmus = UnitConverter.PixelsToEmus(displayHeight);

            // Preserve existing paragraph properties (including alignment)
            var existingParagraphProperties = paragraph.GetFirstChild<W.ParagraphProperties>()?.CloneNode(true) as W.ParagraphProperties;

            // Clear all content from the paragraph
            paragraph.RemoveAllChildren();

            // Restore paragraph properties if they existed
            if (existingParagraphProperties != null)
            {
                paragraph.PrependChild(existingParagraphProperties);
            }

            // Create a new run with the image
            var imageRun = CreateImageRun(documentPart!, imagePath, widthEmus, heightEmus);
            paragraph.AppendChild(imageRun);

            _logger.LogDebug("Replaced image placeholder {ImageName} with {ImagePath} ({Width}x{Height})",
                imageName, imagePath, displayWidth, displayHeight);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace image placeholder {Match}", match.FullMatch);
            return false;
        }
    }

    /// <summary>
    /// Creates a Word run containing an image with the specified dimensions.
    /// </summary>
    private W.Run CreateImageRun(OpenXmlPart documentPart, string imagePath, long widthEmus, long heightEmus)
    {
        // Read image data
        var imageBytes = File.ReadAllBytes(imagePath);

        // Determine the correct image part type
        var imagePartType = ImageTypeDetector.GetImagePartContentType(imagePath) switch
        {
            "image/png" => ImagePartType.Png,
            "image/jpeg" => ImagePartType.Jpeg,
            "image/gif" => ImagePartType.Gif,
            "image/bmp" => ImagePartType.Bmp,
            _ => ImagePartType.Png
        };

        // Add image part to the appropriate document part
        // Each part type (MainDocumentPart, HeaderPart, FooterPart) supports AddImagePart
        ImagePart imagePart = documentPart switch
        {
            MainDocumentPart mainPart => mainPart.AddImagePart(imagePartType),
            HeaderPart headerPart => headerPart.AddImagePart(imagePartType),
            FooterPart footerPart => footerPart.AddImagePart(imagePartType),
            _ => throw new NotSupportedException($"Document part type {documentPart.GetType().Name} does not support image parts")
        };

        using (var stream = new MemoryStream(imageBytes))
        {
            imagePart.FeedData(stream);
        }

        // Get relationship ID
        var relationshipId = documentPart.GetIdOfPart(imagePart);

        // Create the Drawing element
        var drawing = CreateImageDrawing(relationshipId, widthEmus, heightEmus);

        // Create and return the run
        var run = new W.Run();
        run.AppendChild(drawing);

        return run;
    }

    /// <summary>
    /// Creates the OpenXML Drawing element for an image with the specified dimensions.
    /// </summary>
    private W.Drawing CreateImageDrawing(string relationshipId, long widthEmus, long heightEmus)
    {
        var drawing = new W.Drawing(
            new DW.Inline(
                new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties() { Id = (uint)new Random().Next(1, 999999), Name = "Picture" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks() { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties() { Id = 0U, Name = "Image" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip() { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset() { X = 0L, Y = 0L },
                                    new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                            )
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
        );

        return drawing;
    }

    private class ImageOffset
    {
        public int StartIndex { get; init; }
        public int Length { get; init; }
    }
}

/// <summary>
/// Information about a text element and its position in the paragraph
/// </summary>
internal class TextElementInfo
{
    public required W.Text TextElement { get; init; }
    public required int StartPosition { get; set; }
    public required int EndPosition { get; set; }
    public required string OriginalText { get; set; }
}

/// <summary>
/// Information about a found placeholder match
/// </summary>
internal class PlaceholderMatch
{
    public required string PlaceholderName { get; set; }
    public required string PlaceholderKey { get; set; }
    public required string FullMatch { get; set; }
    public required int StartIndex { get; set; }
    public required int Length { get; set; }
    public required PlaceholderType Type { get; set; }
    public required string FilePath { get; set; }
    public required string Section { get; set; }
    public required string Context { get; set; }

    // For image placeholders
    public int? Width { get; set; }
    public int? Height { get; set; }
}

/// <summary>
/// Processing mode for the replacement engine.
/// </summary>
public enum ProcessingMode
{
    Scan,      // Only discover placeholders
    Replace    // Replace placeholders with values
}

/// <summary>
/// Result of processing an entire document.
/// </summary>
public class DocumentProcessingResult
{
    public List<DiscoveredPlaceholder> DiscoveredPlaceholders { get; set; } = new();
    public int ReplacementsPerformed { get; set; }
    public List<SectionProcessingResult> SectionResults { get; } = new();
    public Dictionary<string, int> DetailedReplacements { get; set; } = new();
}

/// <summary>
/// Result of processing a document section.
/// </summary>
public class SectionProcessingResult
{
    public List<DiscoveredPlaceholder> DiscoveredPlaceholders { get; } = new();
    public int ReplacementsPerformed { get; set; }
    public Dictionary<string, int> DetailedReplacements { get; set; } = new();
}

/// <summary>
/// Result of processing a single paragraph.
/// </summary>
public class ParagraphProcessingResult
{
    public List<DiscoveredPlaceholder> DiscoveredPlaceholders { get; set; } = new();
    public int ReplacementsPerformed { get; set; }
    public Dictionary<string, int> DetailedReplacements { get; set; } = new();
}

/// <summary>
/// Information about a discovered placeholder.
/// </summary>
public class DiscoveredPlaceholder
{
    public required string Name { get; init; }
    public required PlaceholderType Type { get; init; }
    public required string Context { get; init; }
    public required string FilePath { get; init; }
}
