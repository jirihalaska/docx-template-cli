using DocxTemplate.Core.Models;
using DocxTemplate.Infrastructure.Images;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Images;

public class ImageProcessorTests
{
    private readonly ImageProcessor _processor;

    public ImageProcessorTests()
    {
        _processor = new ImageProcessor();
    }

    [Fact]
    public void GetImageInfo_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // arrange
        var imagePath = "non-existent-file.png";

        // act & assert
        Assert.Throws<FileNotFoundException>(() => _processor.GetImageInfo(imagePath));
    }

    [Fact]
    public void GetImageInfo_WithInvalidFile_ThrowsInvalidOperationException()
    {
        // arrange
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        File.WriteAllText(tempFile, "not an image");

        try
        {
            // act & assert
            Assert.Throws<InvalidOperationException>(() => _processor.GetImageInfo(tempFile));
        }
        finally
        {
            // cleanup
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(".png", "PNG")]
    [InlineData(".jpg", "JPEG")]
    [InlineData(".jpeg", "JPEG")]
    public void GetImageInfo_DetectsFormat_ReturnsCorrectFormat(string extension, string expectedFormat)
    {
        // arrange
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), extension);
        
        // Create a minimal valid image using SkiaSharp
        using (var bitmap = new SkiaSharp.SKBitmap(10, 10))
        {
            bitmap.Erase(SkiaSharp.SKColors.Red);
            
            var format = extension.ToLowerInvariant() switch
            {
                ".png" => SkiaSharp.SKEncodedImageFormat.Png,
                ".jpg" or ".jpeg" => SkiaSharp.SKEncodedImageFormat.Jpeg,
                _ => SkiaSharp.SKEncodedImageFormat.Png
            };
            
            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var data = image.Encode(format, 100);
            if (data == null)
            {
                // Skip this test if encoding failed
                return;
            }
            File.WriteAllBytes(tempFile, data.ToArray());
        }

        try
        {
            // act
            var result = _processor.GetImageInfo(tempFile);

            // assert
            Assert.Equal(expectedFormat, result.Format);
            Assert.Equal(10, result.Width);
            Assert.Equal(10, result.Height);
        }
        finally
        {
            // cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GetImageInfo_WithUnsupportedFormat_ThrowsNotSupportedException()
    {
        // arrange
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".tiff");
        File.WriteAllText(tempFile, "dummy content");

        try
        {
            // act & assert
            var exception = Assert.Throws<NotSupportedException>(() => _processor.GetImageInfo(tempFile));
            Assert.Contains(".tiff", exception.Message);
        }
        finally
        {
            // cleanup
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetImageInfo_WithCorruptedImageFile_ThrowsInvalidOperationException()
    {
        // arrange
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        File.WriteAllText(tempFile, "not a real image file");

        try
        {
            // act & assert
            var exception = Assert.Throws<InvalidOperationException>(() => _processor.GetImageInfo(tempFile));
            Assert.Contains("Unable to decode image file", exception.Message);
        }
        finally
        {
            // cleanup
            File.Delete(tempFile);
        }
    }
}