using DocxTemplate.Core.Models;
using FluentAssertions;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_WithValidData_ShouldCreateCorrectly()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 2
        };

        // act
        var placeholder = new Placeholder
        {
            Name = "client",
            Pattern = "{{client}}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 2
        };

        // assert
        placeholder.Name.Should().Be("client");
        placeholder.Pattern.Should().Be("{{client}}");
        placeholder.TotalOccurrences.Should().Be(2);
        placeholder.UniqueFileCount.Should().Be(1);
    }

    [Fact]
    public void IsPatternValid_WithValidRegex_ShouldReturnTrue()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Name = "test",
            Pattern = "{{.*}}",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var result = placeholder.IsPatternValid();

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPatternValid_WithInvalidRegex_ShouldReturnFalse()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Name = "test",
            Pattern = "[invalid",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var result = placeholder.IsPatternValid();

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithValidPlaceholder_ShouldReturnTrue()
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
            Name = "client",
            Pattern = "{{client}}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1
        };

        // act
        var result = placeholder.IsValid();

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithMismatchedOccurrences_ShouldReturnFalse()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 2
        };

        var placeholder = new Placeholder
        {
            Name = "client",
            Pattern = "{{client}}",
            Locations = new List<PlaceholderLocation> { location },
            TotalOccurrences = 1 // Mismatch
        };

        // act
        var result = placeholder.IsValid();

        // assert
        result.Should().BeFalse();
    }
}