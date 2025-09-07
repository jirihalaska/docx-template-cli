namespace DocxTemplate.Processing.Images;

/// <summary>
/// Utility class for calculating display dimensions while preserving aspect ratio
/// </summary>
public static class AspectRatioCalculator
{
    /// <summary>
    /// Calculates display dimensions that fit within the specified bounds while preserving aspect ratio
    /// </summary>
    /// <param name="originalWidth">Original image width in pixels</param>
    /// <param name="originalHeight">Original image height in pixels</param>
    /// <param name="maxWidth">Maximum allowed width in pixels</param>
    /// <param name="maxHeight">Maximum allowed height in pixels</param>
    /// <returns>Calculated display dimensions</returns>
    /// <exception cref="ArgumentException">When dimensions are zero or negative</exception>
    public static (int width, int height) CalculateDisplayDimensions(
        int originalWidth, int originalHeight, 
        int maxWidth, int maxHeight)
    {
        if (originalWidth <= 0 || originalHeight <= 0)
        {
            throw new ArgumentException("Original dimensions must be positive");
        }
        
        if (maxWidth <= 0 || maxHeight <= 0)
        {
            throw new ArgumentException("Maximum dimensions must be positive");
        }

        // "contain" mode - fit within bounds preserving aspect ratio
        double scaleWidth = (double)maxWidth / originalWidth;
        double scaleHeight = (double)maxHeight / originalHeight;
        double scale = Math.Min(scaleWidth, scaleHeight);
        
        return ((int)(originalWidth * scale), (int)(originalHeight * scale));
    }
}