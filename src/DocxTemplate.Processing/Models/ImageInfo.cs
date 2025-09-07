namespace DocxTemplate.Processing.Models;

/// <summary>
/// Contains information about an image file
/// </summary>
public record ImageInfo
{
    /// <summary>
    /// The width of the image in pixels
    /// </summary>
    public int Width { get; init; }
    
    /// <summary>
    /// The height of the image in pixels
    /// </summary>
    public int Height { get; init; }
    
    /// <summary>
    /// The format of the image file
    /// </summary>
    public string Format { get; init; } = string.Empty;
}