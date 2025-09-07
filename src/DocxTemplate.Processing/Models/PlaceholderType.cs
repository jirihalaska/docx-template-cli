namespace DocxTemplate.Processing.Models;

/// <summary>
/// Defines the type of placeholder
/// </summary>
public enum PlaceholderType
{
    /// <summary>
    /// Text placeholder for simple text replacement
    /// </summary>
    Text,
    
    /// <summary>
    /// Image placeholder for inserting images with dimensions
    /// </summary>
    Image
}