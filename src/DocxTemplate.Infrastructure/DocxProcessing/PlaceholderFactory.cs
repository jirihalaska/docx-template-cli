using System.Text.RegularExpressions;
using DocxTemplate.Core.Models;

namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Factory for creating placeholder instances from matched patterns
/// </summary>
public static class PlaceholderFactory
{
    /// <summary>
    /// Creates a placeholder from a regex match
    /// </summary>
    /// <param name="match">The regex match</param>
    /// <param name="fullText">The full text containing the placeholder</param>
    /// <param name="filePath">The file path where the placeholder was found</param>
    /// <param name="section">The section of the document</param>
    /// <returns>A placeholder instance or null if invalid</returns>
    public static Placeholder? CreateFromMatch(Match match, string fullText, string filePath, string section)
    {
        if (!match.Success)
            return null;

        var matchValue = match.Value;
        
        // Check if it's an image placeholder
        var imageMatch = Regex.Match(matchValue, PlaceholderPatterns.ImagePlaceholderPattern);
        if (imageMatch.Success)
        {
            return CreateImagePlaceholder(imageMatch, fullText, filePath, section);
        }
        
        // Otherwise, it's a text placeholder
        return CreateTextPlaceholder(matchValue, fullText, filePath, section);
    }
    
    private static Placeholder CreateImagePlaceholder(Match match, string fullText, string filePath, string section)
    {
        var imageName = match.Groups[1].Value;
        var width = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        var height = int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
        
        var fileName = Path.GetFileName(filePath);
        var location = new PlaceholderLocation
        {
            FileName = fileName,
            FilePath = filePath,
            Occurrences = 1,
            Context = $"{section}: {GetContextAroundMatch(fullText, match.Value, 50)}"
        };
        
        return new Placeholder
        {
            Name = imageName,
            Pattern = PlaceholderPatterns.ImagePlaceholderPattern,
            Locations = new[] { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Image,
            ImageProperties = new ImageProperties
            {
                ImageName = imageName,
                MaxWidth = width,
                MaxHeight = height
            }
        };
    }
    
    private static Placeholder CreateTextPlaceholder(string matchValue, string fullText, string filePath, string section)
    {
        // Extract the placeholder name by removing the delimiters
        var name = matchValue.Trim('{', '}').Trim();
        
        var fileName = Path.GetFileName(filePath);
        var location = new PlaceholderLocation
        {
            FileName = fileName,
            FilePath = filePath,
            Occurrences = 1,
            Context = $"{section}: {GetContextAroundMatch(fullText, matchValue, 50)}"
        };
        
        return new Placeholder
        {
            Name = name,
            Pattern = PlaceholderPatterns.TextPlaceholderPattern,
            Locations = new[] { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Text,
            ImageProperties = null
        };
    }
    
    private static string GetContextAroundMatch(string text, string matchValue, int contextLength)
    {
        var index = text.IndexOf(matchValue, StringComparison.Ordinal);
        if (index == -1)
            return string.Empty;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + matchValue.Length + contextLength);
        
        var context = text.Substring(start, end - start);
        
        // Add ellipsis if we truncated
        if (start > 0)
            context = "..." + context;
        if (end < text.Length)
            context = context + "...";

        return context.Trim();
    }
}