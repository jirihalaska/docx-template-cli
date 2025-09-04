using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplate.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Unified processor for handling placeholder identification and text reconstruction
/// across both scanning and replacement operations.
/// </summary>
public class PlaceholderProcessor
{
    private readonly ILogger<PlaceholderProcessor> _logger;
    private static readonly Regex TextPlaceholderPattern = new(PlaceholderPatterns.TextPlaceholderPattern, RegexOptions.Compiled);
    private static readonly Regex ImagePlaceholderPattern = new(PlaceholderPatterns.ImagePlaceholderPattern, RegexOptions.Compiled);

    public PlaceholderProcessor(ILogger<PlaceholderProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reconstructs the full text of a paragraph by joining all text runs.
    /// This is critical for detecting placeholders that are split across multiple runs.
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

    /// <summary>
    /// Builds a detailed map of text elements with their positions in the full paragraph text.
    /// This is used for both scanning (to determine context) and replacement (to update correct elements).
    /// </summary>
    public List<TextElementInfo> BuildTextElementMap(Paragraph paragraph)
    {
        var textElements = new List<TextElementInfo>();
        var runs = paragraph.Descendants<Run>().ToList();
        var currentPos = 0;
        
        foreach (var run in runs)
        {
            var texts = run.Descendants<Text>().ToList();
            foreach (var text in texts)
            {
                var textLength = text.Text?.Length ?? 0;
                textElements.Add(new TextElementInfo
                {
                    TextElement = text,
                    ParentRun = run,
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
    public List<PlaceholderMatch> FindAllPlaceholders(string fullText, string filePath, string section)
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
                    Context = GetContextAroundPlaceholder(fullText, match.Value, 50)
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

    /// <summary>
    /// Replaces a placeholder across multiple text elements by coordinating the replacement
    /// across all affected elements.
    /// </summary>
    public bool ReplaceTextAcrossElements(
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
            _logger.LogWarning("No text elements found for placeholder at position {StartIndex}", startIndex);
            return false;
        }

        try
        {
            // Process affected text elements
            for (int i = 0; i < affectedElements.Count; i++)
            {
                var element = affectedElements[i];
                
                var textStart = Math.Max(0, startIndex - element.StartPosition);
                var textEnd = Math.Min(element.OriginalText.Length, endIndex - element.StartPosition);
                
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

    private class ImageOffset
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }
}

/// <summary>
/// Information about a text element and its position in the paragraph
/// </summary>
public class TextElementInfo
{
    public required Text TextElement { get; set; }
    public required Run ParentRun { get; set; }
    public required int StartPosition { get; set; }
    public required int EndPosition { get; set; }
    public required string OriginalText { get; set; }
}

/// <summary>
/// Information about a found placeholder match
/// </summary>
public class PlaceholderMatch
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
}