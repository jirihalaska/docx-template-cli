using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using SkiaSharp;

namespace DocxTemplate.Infrastructure.Images;

/// <summary>
/// Service for processing image files using SkiaSharp
/// </summary>
public class ImageProcessor : IImageProcessor
{
    /// <summary>
    /// Gets information about an image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>Image information including dimensions and format</returns>
    /// <exception cref="FileNotFoundException">When image file is not found</exception>
    /// <exception cref="NotSupportedException">When image format is not supported</exception>
    /// <exception cref="InvalidOperationException">When image file is corrupted</exception>
    public ImageInfo GetImageInfo(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        // Check format first before attempting to decode
        var format = DetectFormat(imagePath);

        try
        {
            using var image = SKBitmap.Decode(imagePath);
            if (image == null)
            {
                throw new InvalidOperationException($"Unable to decode image file: {imagePath}");
            }

            return new ImageInfo
            {
                Width = image.Width,
                Height = image.Height,
                Format = format
            };
        }
        catch (Exception ex) when (!(ex is FileNotFoundException || ex is NotSupportedException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Error processing image file: {imagePath}", ex);
        }
    }

    private static string DetectFormat(string imagePath)
    {
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "PNG",
            ".jpg" or ".jpeg" => "JPEG", 
            ".gif" => "GIF",
            ".bmp" => "BMP",
            _ => throw new NotSupportedException($"Image format {extension} is not supported")
        };
    }
}