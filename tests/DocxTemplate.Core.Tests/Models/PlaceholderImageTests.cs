using DocxTemplate.Core.Models;
using Xunit;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderImageTests
{
    [Fact]
    public void Placeholder_ShouldDefaultToTextType()
    {
        // arrange & act
        var placeholder = new Placeholder
        {
            Name = "TestPlaceholder",
            Pattern = @"\{\{TestPlaceholder\}\}",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };
        
        // assert
        Assert.Equal(PlaceholderType.Text, placeholder.Type);
        Assert.Null(placeholder.ImageProperties);
    }
    
    [Fact]
    public void Placeholder_ShouldSupportImageType()
    {
        // arrange
        var imageProps = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = "logo"
        };
        
        // act
        var placeholder = new Placeholder
        {
            Name = "logo",
            Pattern = @"\{\{image:logo\|width:800\|height:600\}\}",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0,
            Type = PlaceholderType.Image,
            ImageProperties = imageProps
        };
        
        // assert
        Assert.Equal(PlaceholderType.Image, placeholder.Type);
        Assert.NotNull(placeholder.ImageProperties);
        Assert.Equal(800, placeholder.ImageProperties.MaxWidth);
        Assert.Equal(600, placeholder.ImageProperties.MaxHeight);
        Assert.Equal("logo", placeholder.ImageProperties.ImageName);
    }
    
    [Fact]
    public void Placeholder_IsValid_ShouldFailForImageTypeWithoutProperties()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1
        };
        
        var placeholder = new Placeholder
        {
            Name = "image",
            Pattern = @"\{\{image:.*\}\}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Image,
            ImageProperties = null
        };
        
        // act
        var isValid = placeholder.IsValid();
        
        // assert
        Assert.False(isValid);
    }
    
    [Fact]
    public void Placeholder_IsValid_ShouldFailForImageTypeWithInvalidDimensions()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1
        };
        
        var imageProps = new ImageProperties
        {
            MaxWidth = 0,
            MaxHeight = -1,
            ImageName = "logo"
        };
        
        var placeholder = new Placeholder
        {
            Name = "logo",
            Pattern = @"\{\{image:logo\}\}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Image,
            ImageProperties = imageProps
        };
        
        // act
        var isValid = placeholder.IsValid();
        
        // assert
        Assert.False(isValid);
    }
    
    [Fact]
    public void Placeholder_IsValid_ShouldFailForImageTypeWithEmptyName()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1
        };
        
        var imageProps = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = ""
        };
        
        var placeholder = new Placeholder
        {
            Name = "image",
            Pattern = @"\{\{image:.*\}\}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Image,
            ImageProperties = imageProps
        };
        
        // act
        var isValid = placeholder.IsValid();
        
        // assert
        Assert.False(isValid);
    }
    
    [Fact]
    public void Placeholder_IsValid_ShouldPassForValidImagePlaceholder()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1
        };
        
        var imageProps = new ImageProperties
        {
            MaxWidth = 800,
            MaxHeight = 600,
            ImageName = "logo"
        };
        
        var placeholder = new Placeholder
        {
            Name = "logo",
            Pattern = @"\{\{image:logo\|width:800\|height:600\}\}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Image,
            ImageProperties = imageProps
        };
        
        // act
        var isValid = placeholder.IsValid();
        
        // assert
        Assert.True(isValid);
    }
    
    [Fact]
    public void Placeholder_IsValid_ShouldPassForTextPlaceholderWithoutImageProperties()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1
        };
        
        var placeholder = new Placeholder
        {
            Name = "text",
            Pattern = @"\{\{text\}\}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1,
            Type = PlaceholderType.Text,
            ImageProperties = null
        };
        
        // act
        var isValid = placeholder.IsValid();
        
        // assert
        Assert.True(isValid);
    }
    
    [Theory]
    [InlineData(@"{{image:logo|width:800|height:600}}", "logo", 800, 600)]
    [InlineData(@"{{image:photo|width:1920|height:1080}}", "photo", 1920, 1080)]
    [InlineData(@"{{image:icon|width:32|height:32}}", "icon", 32, 32)]
    public void ImagePlaceholder_ParseSyntax_ShouldExtractCorrectValues(string syntax, string expectedName, int expectedWidth, int expectedHeight)
    {
        // arrange
        var pattern = @"\{\{image:([^|]+)\|width:(\d+)\|height:(\d+)\}\}";
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        
        // act
        var match = regex.Match(syntax);
        
        // assert
        Assert.True(match.Success);
        Assert.Equal(expectedName, match.Groups[1].Value);
        Assert.Equal(expectedWidth.ToString(), match.Groups[2].Value);
        Assert.Equal(expectedHeight.ToString(), match.Groups[3].Value);
    }
}