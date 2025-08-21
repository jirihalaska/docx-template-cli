using DocxTemplate.Infrastructure.Images;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Images;

public class UnitConverterTests
{
    [Theory]
    [InlineData(1, 9525L)]
    [InlineData(0, 0L)]
    [InlineData(10, 95250L)]
    [InlineData(100, 952500L)]
    public void PixelsToEmus_WithValidPixels_ReturnsCorrectEmus(int pixels, long expectedEmus)
    {
        // act
        var result = UnitConverter.PixelsToEmus(pixels);

        // assert
        Assert.Equal(expectedEmus, result);
    }

    [Theory]
    [InlineData(9525L, 1)]
    [InlineData(0L, 0)]
    [InlineData(95250L, 10)]
    [InlineData(952500L, 100)]
    public void EmusToPixels_WithValidEmus_ReturnsCorrectPixels(long emus, int expectedPixels)
    {
        // act
        var result = UnitConverter.EmusToPixels(emus);

        // assert
        Assert.Equal(expectedPixels, result);
    }

    [Fact]
    public void PixelsToEmus_EmusToPixels_RoundTrip_PreservesValue()
    {
        // arrange
        var originalPixels = 42;

        // act
        var emus = UnitConverter.PixelsToEmus(originalPixels);
        var roundTripPixels = UnitConverter.EmusToPixels(emus);

        // assert
        Assert.Equal(originalPixels, roundTripPixels);
    }

    [Theory]
    [InlineData(1920, 1080)] // Common screen resolution
    [InlineData(800, 600)]   // Common image size
    [InlineData(300, 300)]   // Square image
    public void ConversionFactors_WithCommonImageSizes_ProducesReasonableResults(int width, int height)
    {
        // act
        var widthEmus = UnitConverter.PixelsToEmus(width);
        var heightEmus = UnitConverter.PixelsToEmus(height);

        // assert
        Assert.True(widthEmus > 0, "Width EMUs should be positive");
        Assert.True(heightEmus > 0, "Height EMUs should be positive");
        
        // EMUs should be much larger than pixels (9525 times larger)
        Assert.True(widthEmus > width * 1000, "EMUs should be significantly larger than pixels");
        Assert.True(heightEmus > height * 1000, "EMUs should be significantly larger than pixels");
    }
}