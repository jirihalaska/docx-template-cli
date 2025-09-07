using DocxTemplate.Core.Models;
using DocxTemplate.Processing.Models;
using Xunit;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderTypeTests
{
    [Fact]
    public void PlaceholderType_ShouldHaveTextValue()
    {
        // arrange
        const PlaceholderType expected = PlaceholderType.Text;

        // act
        var value = (int)expected;

        // assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void PlaceholderType_ShouldHaveImageValue()
    {
        // arrange
        const PlaceholderType expected = PlaceholderType.Image;

        // act
        var value = (int)expected;

        // assert
        Assert.Equal(1, value);
    }

    [Theory]
    [InlineData(PlaceholderType.Text, "Text")]
    [InlineData(PlaceholderType.Image, "Image")]
    public void PlaceholderType_ShouldHaveCorrectStringRepresentation(PlaceholderType type, string expected)
    {
        // arrange & act
        var result = type.ToString();

        // assert
        Assert.Equal(expected, result);
    }
}
