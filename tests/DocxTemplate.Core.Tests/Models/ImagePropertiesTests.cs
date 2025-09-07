using DocxTemplate.Core.Models;
using DocxTemplate.Processing.Models;
using Xunit;

namespace DocxTemplate.Core.Tests.Models;

public class ImagePropertiesTests
{
    [Fact]
    public void ImageProperties_ShouldInitializeWithRequiredProperties()
    {
        // arrange & act
        var properties = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = "logo"
        };

        // assert
        Assert.Equal(800, properties.MaxWidth);
        Assert.Equal(600, properties.MaxHeight);
        Assert.Equal("logo", properties.ImageName);
    }

    [Fact]
    public void ImageProperties_ShouldBeValueEqual()
    {
        // arrange
        var props1 = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = "logo"
        };

        var props2 = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = "logo"
        };

        // act & assert
        Assert.Equal(props1, props2);
    }

    [Fact]
    public void ImageProperties_ShouldNotBeEqualWithDifferentValues()
    {
        // arrange
        var props1 = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = "logo"
        };

        var props2 = new ImageProperties
        {
            MaxWidth = 1024,
            MaxHeight = 768,
            ImageName = "logo"
        };

        // act & assert
        Assert.NotEqual(props1, props2);
    }

    [Theory]
    [InlineData(1, 1, "test", true)]
    [InlineData(int.MaxValue, int.MaxValue, "test", true)]
    [InlineData(100, 200, "image_name", true)]
    public void ImageProperties_ShouldAcceptValidValues(int width, int height, string name, bool shouldBeValid)
    {
        // arrange & act
        var properties = new ImageProperties
        {
            MaxWidth = width,
            MaxHeight = height,
            ImageName = name
        };

        // assert
        Assert.Equal(shouldBeValid, properties.MaxWidth > 0 && properties.MaxHeight > 0 && !string.IsNullOrWhiteSpace(properties.ImageName));
    }
}
