using DocxTemplate.Core.Models;
using DocxTemplate.Processing.Models;
using FluentAssertions;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderLocationTests
{
    [Fact]
    public void PlaceholderLocation_WithValidData_ShouldCreateCorrectly()
    {
        // arrange & act
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 3,
            Context = "Header section",
            LineNumbers = new List<int> { 1, 5, 10 },
            CharacterPositions = new List<int> { 10, 50, 100 }
        };

        // assert
        location.FileName.Should().Be("test.docx");
        location.FilePath.Should().Be("/path/to/test.docx");
        location.Occurrences.Should().Be(3);
        location.Context.Should().Be("Header section");
        location.LineNumbers.Should().HaveCount(3);
        location.CharacterPositions.Should().HaveCount(3);
    }

    [Fact]
    public void IsValid_WithValidLocation_ShouldReturnTrue()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 2,
            LineNumbers = new List<int> { 1, 5 },
            CharacterPositions = new List<int> { 10, 50 }
        };

        // act
        var result = location.IsValid();

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithZeroOccurrences_ShouldReturnFalse()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 0
        };

        // act
        var result = location.IsValid();

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMismatchedLineNumbers_ShouldReturnFalse()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 2,
            LineNumbers = new List<int> { 1, 5, 10 } // 3 line numbers for 2 occurrences
        };

        // act
        var result = location.IsValid();

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DisplayLocation_ShouldFormatCorrectly()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/to/test.docx",
            Occurrences = 2,
            LineNumbers = new List<int> { 1, 5 }
        };

        // act
        var result = location.DisplayLocation;

        // assert
        result.Should().Be("test.docx (2 occurrences) at lines 1, 5");
    }
}
