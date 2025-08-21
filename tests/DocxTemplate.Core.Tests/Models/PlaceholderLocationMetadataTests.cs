using DocxTemplate.Core.Models;
using Xunit;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderLocationMetadataTests
{
    [Fact]
    public void PlaceholderLocation_ShouldSupportOriginalSyntax()
    {
        // arrange & act
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1,
            OriginalSyntax = "{{image:logo|width:800|height:600}}"
        };
        
        // assert
        Assert.Equal("{{image:logo|width:800|height:600}}", location.OriginalSyntax);
    }
    
    [Fact]
    public void PlaceholderLocation_ShouldSupportMetadata()
    {
        // arrange & act
        var metadata = new Dictionary<string, object>
        {
            ["width"] = 800,
            ["height"] = 600,
            ["imageType"] = "png"
        };
        
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1,
            Metadata = metadata
        };
        
        // assert
        Assert.NotNull(location.Metadata);
        Assert.Equal(3, location.Metadata.Count);
        Assert.Equal(800, location.Metadata["width"]);
        Assert.Equal(600, location.Metadata["height"]);
        Assert.Equal("png", location.Metadata["imageType"]);
    }
    
    [Fact]
    public void PlaceholderLocation_IsValid_ShouldPassWithMetadata()
    {
        // arrange
        var metadata = new Dictionary<string, object>
        {
            ["placeholderType"] = "image",
            ["dimensions"] = "800x600"
        };
        
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1,
            OriginalSyntax = "{{image:test}}",
            Metadata = metadata
        };
        
        // act
        var isValid = location.IsValid();
        
        // assert
        Assert.True(isValid);
    }
    
    [Fact]
    public void PlaceholderLocation_IsValid_ShouldPassWithoutMetadata()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 1,
            OriginalSyntax = null,
            Metadata = null
        };
        
        // act
        var isValid = location.IsValid();
        
        // assert
        Assert.True(isValid);
    }
    
    [Fact]
    public void PlaceholderLocation_ShouldPreserveBackwardsCompatibility()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 2,
            Context = "Found in header",
            LineNumbers = new List<int> { 10, 25 },
            CharacterPositions = new List<int> { 100, 250 }
        };
        
        // act
        var isValid = location.IsValid();
        
        // assert
        Assert.True(isValid);
        Assert.Null(location.OriginalSyntax);
        Assert.Null(location.Metadata);
    }
}