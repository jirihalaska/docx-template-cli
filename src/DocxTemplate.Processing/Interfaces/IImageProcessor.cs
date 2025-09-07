using DocxTemplate.Processing.Models;

namespace DocxTemplate.Processing.Interfaces;

/// <summary>
/// Service for processing image files
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Gets information about an image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>Image information including dimensions and format</returns>
    /// <exception cref="FileNotFoundException">When image file is not found</exception>
    /// <exception cref="NotSupportedException">When image format is not supported</exception>
    /// <exception cref="InvalidOperationException">When image file is corrupted</exception>
    ImageInfo GetImageInfo(string imagePath);
}