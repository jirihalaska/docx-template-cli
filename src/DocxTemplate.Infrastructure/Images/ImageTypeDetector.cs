using DocumentFormat.OpenXml.Packaging;

namespace DocxTemplate.Infrastructure.Images;

/// <summary>
/// Utility class for detecting image types and mapping to DocumentFormat.OpenXml types
/// </summary>
public static class ImageTypeDetector
{
    /// <summary>
    /// Gets the content type string for creating ImagePart for a given image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>The appropriate content type for DocumentFormat.OpenXml</returns>
    /// <exception cref="NotSupportedException">When image format is not supported</exception>
    public static string GetImagePartContentType(string imagePath)
    {
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => ImagePartType.Png.ContentType,
            ".jpg" or ".jpeg" => ImagePartType.Jpeg.ContentType,
            ".gif" => ImagePartType.Gif.ContentType,
            ".bmp" => ImagePartType.Bmp.ContentType,
            _ => throw new NotSupportedException($"Image type {extension} not supported")
        };
    }

    /// <summary>
    /// Gets the content type string for a given image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>The MIME content type</returns>
    /// <exception cref="NotSupportedException">When image format is not supported</exception>
    public static string GetContentType(string imagePath)
    {
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif", 
            ".bmp" => "image/bmp",
            _ => throw new NotSupportedException($"Image type {extension} not supported")
        };
    }
}