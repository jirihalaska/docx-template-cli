using DocxTemplate.Core.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Tests.Models;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // arrange
        var pattern = "{{name}}";
        var name = "name";
        var locations = new List<PlaceholderLocation>
        {
            new() { FileName = "doc1.docx", FilePath = "/doc1.docx", Occurrences = 1, Context = "Hello {{name}}" }
        };
        var totalOccurrences = 1;

        // act
        var placeholder = new Placeholder
        {
            Pattern = pattern,
            Name = name,
            Locations = locations,
            TotalOccurrences = totalOccurrences
        };

        // assert
        placeholder.Pattern.Should().Be(pattern);
        placeholder.Name.Should().Be(name);
        placeholder.Locations.Should().BeEquivalentTo(locations);
        placeholder.TotalOccurrences.Should().Be(totalOccurrences);
    }

    [Fact]
    public void Placeholder_UniqueFileCount_ShouldReturnNumberOfUniqueFiles()
    {
        // arrange
        var locations = new List<PlaceholderLocation>
        {
            new() { FileName = "doc1.docx", FilePath = "/doc1.docx", Occurrences = 2, Context = "Hello {{name}}, Welcome {{name}}" },
            new() { FileName = "doc2.docx", FilePath = "/doc2.docx", Occurrences = 1, Context = "Hi {{name}}" }
        };

        var placeholder = new Placeholder
        {
            Pattern = "{{name}}",
            Name = "name",
            Locations = locations,
            TotalOccurrences = 3
        };

        // act
        var uniqueFileCount = placeholder.UniqueFileCount;

        // assert
        uniqueFileCount.Should().Be(2);
    }

    [Fact]
    public void Placeholder_IsPatternValid_ShouldReturnTrueForValidRegex()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = @"\{\{[a-zA-Z_][a-zA-Z0-9_]*\}\}",
            Name = "validPattern",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var isValid = placeholder.IsPatternValid();

        // assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("[invalid")]
    [InlineData("*invalid")]
    [InlineData("(unclosed")]
    public void Placeholder_IsPatternValid_ShouldReturnFalseForInvalidRegex(string invalidPattern)
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = invalidPattern,
            Name = "invalidPattern",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var isValid = placeholder.IsPatternValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Placeholder_IsValid_ShouldReturnTrueForValidData()
    {
        // arrange
        var locations = new List<PlaceholderLocation>
        {
            new() { FileName = "doc1.docx", FilePath = "/doc1.docx", Occurrences = 2, Context = "Hello {{name}}, Welcome {{name}}" },
            new() { FileName = "doc2.docx", FilePath = "/doc2.docx", Occurrences = 1, Context = "Hi {{name}}" }
        };

        var placeholder = new Placeholder
        {
            Pattern = "{{name}}",
            Name = "name",
            Locations = locations,
            TotalOccurrences = 3
        };

        // act
        var isValid = placeholder.IsValid();

        // assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Placeholder_IsValid_ShouldReturnFalseWhenTotalOccurrencesMismatch()
    {
        // arrange
        var locations = new List<PlaceholderLocation>
        {
            new() { FileName = "doc1.docx", FilePath = "/doc1.docx", Occurrences = 2, Context = "Hello {{name}}, Welcome {{name}}" }
        };

        var placeholder = new Placeholder
        {
            Pattern = "{{name}}",
            Name = "name",
            Locations = locations,
            TotalOccurrences = 5 // Wrong total
        };

        // act
        var isValid = placeholder.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Placeholder_IsValid_ShouldReturnFalseForInvalidPattern()
    {
        // arrange
        var locations = new List<PlaceholderLocation>
        {
            new() { FileName = "doc1.docx", FilePath = "/doc1.docx", Occurrences = 1, Context = "Hello {{name}}" }
        };

        var placeholder = new Placeholder
        {
            Pattern = "[invalid", // Invalid regex
            Name = "name",
            Locations = locations,
            TotalOccurrences = 1
        };

        // act
        var isValid = placeholder.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Placeholder_Validation_ShouldFailWithEmptyPattern()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = string.Empty,
            Name = "name",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(placeholder, new ValidationContext(placeholder), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("pattern is required"));
    }

    [Fact]
    public void Placeholder_Validation_ShouldFailWithEmptyName()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = "{{test}}",
            Name = string.Empty,
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(placeholder, new ValidationContext(placeholder), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("name is required"));
    }

    [Fact]
    public void Placeholder_Validation_ShouldFailWithTooLongName()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = "{{test}}",
            Name = new string('a', 201), // Too long
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = 0
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(placeholder, new ValidationContext(placeholder), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("between 1 and 200 characters"));
    }

    [Fact]
    public void Placeholder_Validation_ShouldFailWithNegativeTotalOccurrences()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = "{{test}}",
            Name = "test",
            Locations = new List<PlaceholderLocation>(),
            TotalOccurrences = -1
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(placeholder, new ValidationContext(placeholder), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("non-negative"));
    }

    [Fact]
    public void Placeholder_Validation_ShouldPassWithValidData()
    {
        // arrange
        var placeholder = new Placeholder
        {
            Pattern = "{{name}}",
            Name = "name",
            Locations = new List<PlaceholderLocation>
            {
                new() { FileName = "doc.docx", FilePath = "/doc.docx", Occurrences = 1, Context = "{{name}}" }
            },
            TotalOccurrences = 1
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(placeholder, new ValidationContext(placeholder), validationResults, true);

        // assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}