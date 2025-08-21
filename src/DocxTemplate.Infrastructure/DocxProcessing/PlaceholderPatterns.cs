namespace DocxTemplate.Infrastructure.DocxProcessing;

/// <summary>
/// Contains regex patterns for different placeholder types
/// </summary>
public static class PlaceholderPatterns
{
    /// <summary>
    /// Pattern for image placeholders: {{image:name|width:pixels|height:pixels}}
    /// </summary>
    public const string ImagePlaceholderPattern = @"\{\{image:([^|]+)\|width:(\d+)\|height:(\d+)\}\}";
    
    /// <summary>
    /// Pattern for text placeholders: {{name}}
    /// </summary>
    public const string TextPlaceholderPattern = @"\{\{([^}]+)\}\}";
    
    /// <summary>
    /// Combined pattern that matches both text and image placeholders
    /// </summary>
    public const string CombinedPlaceholderPattern = @"\{\{(?:image:[^|]+\|width:\d+\|height:\d+|[^}]+)\}\}";
}