using System.Text.RegularExpressions;
using DocxTemplate.Processing;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.DocxProcessing;

public class PlaceholderPatternsTests
{
    [Theory]
    [InlineData("{{image:logo|width:200|height:100}}", true, "logo", "200", "100")]
    [InlineData("{{image:company_logo|width:1920|height:1080}}", true, "company_logo", "1920", "1080")]
    [InlineData("{{image:photo-1|width:50|height:50}}", true, "photo-1", "50", "50")]
    [InlineData("{{image:my.image|width:300|height:200}}", true, "my.image", "300", "200")]
    public void ImagePlaceholderPattern_MatchesValidImagePlaceholders(
        string input,
        bool shouldMatch,
        string expectedName,
        string expectedWidth,
        string expectedHeight)
    {
        // arrange
        var regex = new Regex(PlaceholderPatterns.ImagePlaceholderPattern);
        
        // act
        var match = regex.Match(input);
        
        // assert
        Assert.Equal(shouldMatch, match.Success);
        if (shouldMatch)
        {
            Assert.Equal(expectedName, match.Groups[1].Value);
            Assert.Equal(expectedWidth, match.Groups[2].Value);
            Assert.Equal(expectedHeight, match.Groups[3].Value);
        }
    }

    [Theory]
    [InlineData("{{image:logo}}", false)]  // Missing width and height
    [InlineData("{{image:logo|width:200}}", false)]  // Missing height
    [InlineData("{{image:logo|height:100}}", false)]  // Missing width
    [InlineData("{{logo|width:200|height:100}}", false)]  // Missing image: prefix
    [InlineData("{{image:|width:200|height:100}}", false)]  // Empty image name
    [InlineData("{{image:logo|width:abc|height:100}}", false)]  // Non-numeric width
    [InlineData("{{image:logo|width:200|height:xyz}}", false)]  // Non-numeric height
    public void ImagePlaceholderPattern_DoesNotMatchInvalidImagePlaceholders(string input, bool shouldMatch)
    {
        // arrange
        var regex = new Regex(PlaceholderPatterns.ImagePlaceholderPattern);
        
        // act
        var match = regex.Match(input);
        
        // assert
        Assert.Equal(shouldMatch, match.Success);
    }

    [Theory]
    [InlineData("{{name}}", true, "name")]
    [InlineData("{{SOUBOR_PREFIX}}", true, "SOUBOR_PREFIX")]
    [InlineData("{{user_name}}", true, "user_name")]
    [InlineData("{{field-1}}", true, "field-1")]
    [InlineData("{{my.field}}", true, "my.field")]
    public void TextPlaceholderPattern_MatchesValidTextPlaceholders(
        string input,
        bool shouldMatch,
        string expectedName)
    {
        // arrange
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern);
        
        // act
        var match = regex.Match(input);
        
        // assert
        Assert.Equal(shouldMatch, match.Success);
        if (shouldMatch)
        {
            Assert.Equal(expectedName, match.Groups[1].Value);
        }
    }

    [Theory]
    [InlineData("{name}", false)]  // Single braces
    [InlineData("{{name", false)]  // Missing closing braces
    [InlineData("name}}", false)]  // Missing opening braces
    [InlineData("{{}}", false)]  // Empty placeholder
    public void TextPlaceholderPattern_DoesNotMatchInvalidTextPlaceholders(string input, bool shouldMatch)
    {
        // arrange
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern);
        
        // act
        var match = regex.Match(input);
        
        // assert
        Assert.Equal(shouldMatch, match.Success);
    }

    [Theory]
    [InlineData("{{name}}", true)]
    [InlineData("{{image:logo|width:200|height:100}}", true)]
    [InlineData("{{SOUBOR_PREFIX}}", true)]
    [InlineData("Regular text", false)]
    [InlineData("{single}", false)]
    public void CombinedPlaceholderPattern_MatchesBothTypes(string input, bool shouldMatch)
    {
        // arrange
        var regex = new Regex(PlaceholderPatterns.CombinedPlaceholderPattern);
        
        // act
        var match = regex.Match(input);
        
        // assert
        Assert.Equal(shouldMatch, match.Success);
    }

    [Fact]
    public void CombinedPlaceholderPattern_MatchesMultiplePlaceholdersInText()
    {
        // arrange
        var text = "Hello {{name}}, here is an {{image:logo|width:100|height:50}} for you.";
        var regex = new Regex(PlaceholderPatterns.CombinedPlaceholderPattern);
        
        // act
        var matches = regex.Matches(text);
        
        // assert
        Assert.Equal(2, matches.Count);
        Assert.Equal("{{name}}", matches[0].Value);
        Assert.Equal("{{image:logo|width:100|height:50}}", matches[1].Value);
    }
}