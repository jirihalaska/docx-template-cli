using DocumentFormat.OpenXml.Packaging;
using DocxTemplate.Infrastructure.Images;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Images;

public class ImageTypeDetectorTests
{
    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.gif", "image/gif")]
    [InlineData("image.bmp", "image/bmp")]
    [InlineData("IMAGE.PNG", "image/png")] // Case insensitive
    [InlineData("/path/to/image.jpg", "image/jpeg")] // With path
    public void GetImagePartContentType_WithSupportedFormats_ReturnsCorrectContentType(string imagePath, string expectedContentType)
    {
        // act
        var result = ImageTypeDetector.GetImagePartContentType(imagePath);

        // assert
        Assert.Equal(expectedContentType, result);
    }

    [Theory]
    [InlineData("image.tiff")]
    [InlineData("image.svg")]
    [InlineData("image.webp")]
    [InlineData("image.txt")]
    [InlineData("image")]
    public void GetImagePartContentType_WithUnsupportedFormats_ThrowsNotSupportedException(string imagePath)
    {
        // act & assert
        Assert.Throws<NotSupportedException>(() => ImageTypeDetector.GetImagePartContentType(imagePath));
    }

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.gif", "image/gif")]
    [InlineData("image.bmp", "image/bmp")]
    [InlineData("IMAGE.PNG", "image/png")] // Case insensitive
    public void GetContentType_WithSupportedFormats_ReturnsCorrectMimeType(string imagePath, string expectedContentType)
    {
        // act
        var result = ImageTypeDetector.GetContentType(imagePath);

        // assert
        Assert.Equal(expectedContentType, result);
    }

    [Theory]
    [InlineData("image.tiff")]
    [InlineData("image.svg")]
    [InlineData("image.webp")]
    public void GetContentType_WithUnsupportedFormats_ThrowsNotSupportedException(string imagePath)
    {
        // act & assert
        Assert.Throws<NotSupportedException>(() => ImageTypeDetector.GetContentType(imagePath));
    }
}