using DocxTemplate.Core.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderLocationTests
{
    [Fact]
    public void PlaceholderLocation_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // arrange
        var fileName = "document.docx";
        var filePath = "/path/to/document.docx";
        var occurrences = 3;
        var context = "Hello {{name}}, welcome {{name}}, goodbye {{name}}";
        var lineNumbers = new List<int> { 1, 5, 10 };
        var characterPositions = new List<int> { 10, 50, 120 };

        // act
        var location = new PlaceholderLocation
        {
            FileName = fileName,
            FilePath = filePath,
            Occurrences = occurrences,
            Context = context,
            LineNumbers = lineNumbers,
            CharacterPositions = characterPositions
        };

        // assert
        location.FileName.Should().Be(fileName);
        location.FilePath.Should().Be(filePath);
        location.Occurrences.Should().Be(occurrences);
        location.Context.Should().Be(context);
        location.LineNumbers.Should().BeEquivalentTo(lineNumbers);
        location.CharacterPositions.Should().BeEquivalentTo(characterPositions);
    }

    [Fact]
    public void PlaceholderLocation_DisplayLocation_ShouldFormatCorrectlyWithSingleOccurrence()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/test.docx",
            Occurrences = 1,
            LineNumbers = new List<int> { 5 }
        };

        // act
        var display = location.DisplayLocation;

        // assert
        display.Should().Be("test.docx (1 occurrence) at line 5");
    }

    [Fact]
    public void PlaceholderLocation_DisplayLocation_ShouldFormatCorrectlyWithMultipleOccurrences()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/test.docx",
            Occurrences = 3,
            LineNumbers = new List<int> { 1, 5, 10 }
        };

        // act
        var display = location.DisplayLocation;

        // assert
        display.Should().Be("test.docx (3 occurrences) at lines 1, 5, 10");
    }

    [Fact]
    public void PlaceholderLocation_DisplayLocation_ShouldFormatCorrectlyWithoutLineNumbers()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/test.docx",
            Occurrences = 2
        };

        // act
        var display = location.DisplayLocation;

        // assert
        display.Should().Be("test.docx (2 occurrences)");
    }

    [Fact]
    public void PlaceholderLocation_IsValid_ShouldReturnTrueForValidData()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 2,
            LineNumbers = new List<int> { 1, 5 },
            CharacterPositions = new List<int> { 10, 50 }
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "/path/test.docx")]
    [InlineData("   ", "/path/test.docx")]
    [InlineData("test.docx", "")]
    [InlineData("test.docx", "   ")]
    public void PlaceholderLocation_IsValid_ShouldReturnFalseForEmptyRequiredFields(string fileName, string filePath)
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = fileName,
            FilePath = filePath,
            Occurrences = 1
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PlaceholderLocation_IsValid_ShouldReturnFalseForZeroOccurrences()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 0
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PlaceholderLocation_IsValid_ShouldReturnFalseWhenLineNumbersCountMismatch()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 2,
            LineNumbers = new List<int> { 1, 5, 10 } // 3 line numbers but 2 occurrences
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PlaceholderLocation_IsValid_ShouldReturnFalseWhenCharacterPositionsCountMismatch()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 2,
            CharacterPositions = new List<int> { 10 } // 1 position but 2 occurrences
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PlaceholderLocation_IsValid_ShouldReturnFalseForNonPositiveLineNumbers()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 2,
            LineNumbers = new List<int> { 0, 5 } // 0 is invalid
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PlaceholderLocation_IsValid_ShouldReturnFalseForNegativeCharacterPositions()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 2,
            CharacterPositions = new List<int> { -1, 50 } // -1 is invalid
        };

        // act
        var isValid = location.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PlaceholderLocation_Validation_ShouldFailWithEmptyFileName()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = string.Empty,
            FilePath = "/path/test.docx",
            Occurrences = 1
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(location, new ValidationContext(location), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("File name is required"));
    }

    [Fact]
    public void PlaceholderLocation_Validation_ShouldFailWithEmptyFilePath()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = string.Empty,
            Occurrences = 1
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(location, new ValidationContext(location), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("File path is required"));
    }

    [Fact]
    public void PlaceholderLocation_Validation_ShouldFailWithZeroOccurrences()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 0
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(location, new ValidationContext(location), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("Occurrences must be at least 1"));
    }

    [Fact]
    public void PlaceholderLocation_Validation_ShouldPassWithValidData()
    {
        // arrange
        var location = new PlaceholderLocation
        {
            FileName = "test.docx",
            FilePath = "/path/test.docx",
            Occurrences = 2,
            Context = "Hello {{name}}, welcome {{name}}",
            LineNumbers = new List<int> { 1, 3 },
            CharacterPositions = new List<int> { 10, 50 }
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(location, new ValidationContext(location), validationResults, true);

        // assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}