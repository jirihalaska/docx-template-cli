using System.Text.RegularExpressions;
using DocxTemplate.Core.Models;
using DocxTemplate.Infrastructure.DocxProcessing;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.DocxProcessing;

public class PlaceholderFactoryTests
{
    [Fact]
    public void CreateFromMatch_CreatesImagePlaceholder_FromValidImageMatch()
    {
        // arrange
        var fullText = "Document with {{image:logo|width:200|height:100}} placeholder";
        var regex = new Regex(PlaceholderPatterns.CombinedPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Equal("logo", placeholder.Name);
        Assert.Equal(PlaceholderType.Image, placeholder.Type);
        Assert.NotNull(placeholder.ImageProperties);
        Assert.Equal("logo", placeholder.ImageProperties.ImageName);
        Assert.Equal(200, placeholder.ImageProperties.MaxWidth);
        Assert.Equal(100, placeholder.ImageProperties.MaxHeight);
        Assert.Single(placeholder.Locations);
        Assert.Equal("file.docx", placeholder.Locations[0].FileName);
    }

    [Fact]
    public void CreateFromMatch_CreatesTextPlaceholder_FromValidTextMatch()
    {
        // arrange
        var fullText = "Document with {{username}} placeholder";
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Equal("username", placeholder.Name);
        Assert.Equal(PlaceholderType.Text, placeholder.Type);
        Assert.Null(placeholder.ImageProperties);
        Assert.Single(placeholder.Locations);
        Assert.Equal("file.docx", placeholder.Locations[0].FileName);
    }

    [Fact]
    public void CreateFromMatch_ReturnsNull_ForUnsuccessfulMatch()
    {
        // arrange
        var fullText = "Document without placeholders";
        var regex = new Regex(PlaceholderPatterns.CombinedPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.Null(placeholder);
    }

    [Fact]
    public void CreateFromMatch_IncludesContext_InLocation()
    {
        // arrange
        var fullText = "This is some text before the {{placeholder}} and some text after it for context";
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Header");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Single(placeholder.Locations);
        var location = placeholder.Locations[0];
        Assert.Contains("Header:", location.Context);
        Assert.Contains("before the {{placeholder}} and some text after", location.Context);
    }

    [Fact]
    public void CreateFromMatch_HandlesLongContext_WithEllipsis()
    {
        // arrange
        var longTextBefore = new string('x', 100);
        var longTextAfter = new string('y', 100);
        var fullText = $"{longTextBefore}{{{{placeholder}}}}{longTextAfter}";
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Single(placeholder.Locations);
        var context = placeholder.Locations[0].Context;
        Assert.StartsWith("Body: ...", context);
        Assert.EndsWith("...", context);
    }

    [Fact]
    public void CreateFromMatch_ParsesComplexImageName()
    {
        // arrange
        var fullText = "Document with {{image:company-logo_v2.1|width:1920|height:1080}} placeholder";
        var regex = new Regex(PlaceholderPatterns.CombinedPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Equal("company-logo_v2.1", placeholder.Name);
        Assert.Equal(PlaceholderType.Image, placeholder.Type);
        Assert.NotNull(placeholder.ImageProperties);
        Assert.Equal(1920, placeholder.ImageProperties.MaxWidth);
        Assert.Equal(1080, placeholder.ImageProperties.MaxHeight);
    }

    [Fact]
    public void CreateFromMatch_SetsCorrectPattern_ForImagePlaceholder()
    {
        // arrange
        var fullText = "{{image:test|width:100|height:200}}";
        var regex = new Regex(PlaceholderPatterns.CombinedPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Equal(PlaceholderPatterns.ImagePlaceholderPattern, placeholder.Pattern);
    }

    [Fact]
    public void CreateFromMatch_SetsCorrectPattern_ForTextPlaceholder()
    {
        // arrange
        var fullText = "{{test}}";
        var regex = new Regex(PlaceholderPatterns.TextPlaceholderPattern);
        var match = regex.Match(fullText);
        
        // act
        var placeholder = PlaceholderFactory.CreateFromMatch(match, fullText, "/path/to/file.docx", "Body");
        
        // assert
        Assert.NotNull(placeholder);
        Assert.Equal(PlaceholderPatterns.TextPlaceholderPattern, placeholder.Pattern);
    }
}