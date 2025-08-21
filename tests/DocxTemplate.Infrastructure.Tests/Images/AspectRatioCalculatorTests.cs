using DocxTemplate.Infrastructure.Images;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Images;

public class AspectRatioCalculatorTests
{
    [Theory]
    [InlineData(100, 100, 50, 50, 50, 50)] // Square image, square bounds
    [InlineData(200, 100, 100, 100, 100, 50)] // Wide image, square bounds
    [InlineData(100, 200, 100, 100, 50, 100)] // Tall image, square bounds
    [InlineData(400, 200, 200, 150, 200, 100)] // Wide image, wide bounds - width constrained
    [InlineData(200, 400, 150, 200, 100, 200)] // Tall image, tall bounds - height constrained
    public void CalculateDisplayDimensions_WithValidDimensions_ReturnsCorrectAspectRatio(
        int originalWidth, int originalHeight, 
        int maxWidth, int maxHeight, 
        int expectedWidth, int expectedHeight)
    {
        // act
        var (width, height) = AspectRatioCalculator.CalculateDisplayDimensions(
            originalWidth, originalHeight, maxWidth, maxHeight);

        // assert
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        
        // Verify aspect ratio is preserved (within rounding tolerance)
        var originalRatio = (double)originalWidth / originalHeight;
        var resultRatio = (double)width / height;
        Assert.True(Math.Abs(originalRatio - resultRatio) < 0.01, 
            $"Aspect ratio not preserved. Original: {originalRatio:F3}, Result: {resultRatio:F3}");
    }

    [Theory]
    [InlineData(0, 100, 100, 100)]
    [InlineData(-10, 100, 100, 100)]
    [InlineData(100, 0, 100, 100)]
    [InlineData(100, -10, 100, 100)]
    public void CalculateDisplayDimensions_WithInvalidOriginalDimensions_ThrowsArgumentException(
        int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        // act & assert
        Assert.Throws<ArgumentException>(() => 
            AspectRatioCalculator.CalculateDisplayDimensions(originalWidth, originalHeight, maxWidth, maxHeight));
    }

    [Theory]
    [InlineData(100, 100, 0, 100)]
    [InlineData(100, 100, -10, 100)]
    [InlineData(100, 100, 100, 0)]
    [InlineData(100, 100, 100, -10)]
    public void CalculateDisplayDimensions_WithInvalidMaxDimensions_ThrowsArgumentException(
        int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        // act & assert
        Assert.Throws<ArgumentException>(() => 
            AspectRatioCalculator.CalculateDisplayDimensions(originalWidth, originalHeight, maxWidth, maxHeight));
    }

    [Fact]
    public void CalculateDisplayDimensions_WithExactFit_ReturnsOriginalDimensions()
    {
        // arrange
        var originalWidth = 100;
        var originalHeight = 50;
        var maxWidth = 100;
        var maxHeight = 50;

        // act
        var (width, height) = AspectRatioCalculator.CalculateDisplayDimensions(
            originalWidth, originalHeight, maxWidth, maxHeight);

        // assert
        Assert.Equal(originalWidth, width);
        Assert.Equal(originalHeight, height);
    }
}