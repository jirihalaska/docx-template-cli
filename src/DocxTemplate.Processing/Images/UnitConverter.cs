namespace DocxTemplate.Processing.Images;

/// <summary>
/// Utility class for converting between different measurement units used in Word documents
/// </summary>
public static class UnitConverter
{
    /// <summary>
    /// Conversion factor from pixels to English Metric Units (EMUs)
    /// </summary>
    private const int EmusPerPixel = 9525;

    /// <summary>
    /// Converts pixels to English Metric Units (EMUs) used by Word documents
    /// </summary>
    /// <param name="pixels">Value in pixels</param>
    /// <returns>Value in EMUs</returns>
    public static long PixelsToEmus(int pixels)
    {
        return (long)pixels * EmusPerPixel;
    }

    /// <summary>
    /// Converts English Metric Units (EMUs) to pixels
    /// </summary>
    /// <param name="emus">Value in EMUs</param>
    /// <returns>Value in pixels</returns>
    public static int EmusToPixels(long emus)
    {
        return (int)(emus / EmusPerPixel);
    }
}