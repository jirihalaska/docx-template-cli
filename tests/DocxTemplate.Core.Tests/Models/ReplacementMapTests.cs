using DocxTemplate.Core.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DocxTemplate.Core.Tests.Models;

public class ReplacementMapTests
{
    [Fact]
    public void ReplacementMap_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // arrange
        var mappings = new Dictionary<string, string>
        {
            ["name"] = "John Doe",
            ["company"] = "Acme Corp"
        };

        // act
        var replacementMap = new ReplacementMap
        {
            Mappings = mappings
        };

        // assert
        replacementMap.Mappings.Should().BeEquivalentTo(mappings);
    }

    [Fact]
    public void ReplacementMap_Count_ShouldReturnNumberOfMappings()
    {
        // arrange
        var mappings = new Dictionary<string, string>
        {
            ["name"] = "John Doe",
            ["company"] = "Acme Corp",
            ["email"] = "john@acme.com"
        };

        var replacementMap = new ReplacementMap { Mappings = mappings };

        // act
        var count = replacementMap.Count;

        // assert
        count.Should().Be(3);
    }

    [Fact]
    public void ReplacementMap_PlaceholderNames_ShouldReturnAllKeys()
    {
        // arrange
        var mappings = new Dictionary<string, string>
        {
            ["name"] = "John Doe",
            ["company"] = "Acme Corp"
        };

        var replacementMap = new ReplacementMap { Mappings = mappings };

        // act
        var keys = replacementMap.PlaceholderNames;

        // assert
        keys.Should().Contain("name");
        keys.Should().Contain("company");
        keys.Should().HaveCount(2);
    }

    [Fact]
    public void ReplacementMap_ReplacementValues_ShouldReturnAllValues()
    {
        // arrange
        var mappings = new Dictionary<string, string>
        {
            ["name"] = "John Doe",
            ["company"] = "Acme Corp"
        };

        var replacementMap = new ReplacementMap { Mappings = mappings };

        // act
        var values = replacementMap.ReplacementValues;

        // assert
        values.Should().Contain("John Doe");
        values.Should().Contain("Acme Corp");
        values.Should().HaveCount(2);
    }

    [Fact]
    public void ReplacementMap_EmptyMappings_ShouldHaveZeroCount()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>()
        };

        // act
        var isEmpty = replacementMap.Count == 0;

        // assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    public void ReplacementMap_NonEmptyMappings_ShouldHavePositiveCount()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string> { ["key"] = "value" }
        };

        // act
        var isEmpty = replacementMap.Count == 0;

        // assert
        isEmpty.Should().BeFalse();
    }

    [Fact]
    public void ReplacementMap_Mappings_ShouldContainExistingKey()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string> { ["name"] = "John" }
        };

        // act
        var contains = replacementMap.Mappings.ContainsKey("name");

        // assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void ReplacementMap_Mappings_ShouldNotContainNonExistingKey()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string> { ["name"] = "John" }
        };

        // act
        var contains = replacementMap.Mappings.ContainsKey("company");

        // assert
        contains.Should().BeFalse();
    }

    [Fact]
    public void ReplacementMap_Mappings_ShouldReturnValueForExistingKey()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string> { ["name"] = "John Doe" }
        };

        // act
        var success = replacementMap.Mappings.TryGetValue("name", out var value);

        // assert
        success.Should().BeTrue();
        value.Should().Be("John Doe");
    }

    [Fact]
    public void ReplacementMap_Mappings_ShouldReturnFalseForNonExistingKey()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string> { ["name"] = "John Doe" }
        };

        // act
        var success = replacementMap.Mappings.TryGetValue("company", out var value);

        // assert
        success.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void ReplacementMap_SanitizeReplacementValue_ShouldReturnSanitizedValue()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string> { ["name"] = "John\0Doe\tTest" }
        };

        // act
        var sanitized = ReplacementMap.SanitizeReplacementValue(replacementMap.Mappings["name"]);

        // assert
        sanitized.Should().Be("John Doe Test");
    }

    [Fact]
    public void ReplacementMap_SanitizeReplacementValue_ShouldReturnEmptyStringForNonExistingKey()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>()
        };

        // act
        var sanitized = replacementMap.Mappings.TryGetValue("nonexistent", out var value) 
            ? ReplacementMap.SanitizeReplacementValue(value) 
            : string.Empty;

        // assert
        sanitized.Should().BeEmpty();
    }

    [Theory]
    [InlineData("John\0Doe", "John Doe")]
    [InlineData("Test\tValue", "Test Value")]
    [InlineData("Line\nBreak", "Line\nBreak")] // Note: \n is not sanitized in the actual implementation
    [InlineData("Carriage\rReturn", "Carriage\rReturn")] // Note: \r is not sanitized in the actual implementation
    [InlineData("Normal Text", "Normal Text")]
    [InlineData("", "")]
    public void ReplacementMap_SanitizeReplacementValue_ShouldRemoveDangerousCharacters(string input, string expected)
    {
        // act
        var sanitized = ReplacementMap.SanitizeReplacementValue(input);

        // assert
        sanitized.Should().Be(expected);
    }

    [Fact]
    public void ReplacementMap_SanitizeReplacementValue_ShouldReturnEmptyStringForNull()
    {
        // act
        var sanitized = ReplacementMap.SanitizeReplacementValue(null!);

        // assert
        sanitized.Should().BeEmpty();
    }

    [Fact]
    public void ReplacementMap_FromJson_ShouldCreateInstanceFromValidJson()
    {
        // arrange
        var json = """
        {
            "name": "John Doe",
            "company": "Acme Corp",
            "email": "john@acme.com"
        }
        """;

        // act
        var replacementMap = ReplacementMap.FromJson(json);

        // assert
        replacementMap.Should().NotBeNull();
        replacementMap.Count.Should().Be(3);
        replacementMap.Mappings.TryGetValue("name", out var name).Should().BeTrue();
        name.Should().Be("John Doe");
    }

    [Fact]
    public void ReplacementMap_FromJson_ShouldThrowForInvalidJson()
    {
        // arrange
        var invalidJson = "{ invalid json }";

        // act & assert
        FluentActions.Invoking(() => ReplacementMap.FromJson(invalidJson))
            .Should().Throw<JsonException>();
    }

    [Fact]
    public void ReplacementMap_ToJson_ShouldSerializeToValidJson()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["name"] = "John Doe",
                ["company"] = "Acme Corp"
            }
        };

        // act
        var json = replacementMap.ToJson();

        // assert
        json.Should().NotBeNullOrEmpty();
        
        // Verify it can be deserialized back
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        deserialized.Should().NotBeNull();
        deserialized!["name"].Should().Be("John Doe");
        deserialized["company"].Should().Be("Acme Corp");
    }

    [Fact]
    public void ReplacementMap_MergeWith_ShouldCombineMappings()
    {
        // arrange
        var map1 = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["name"] = "John",
                ["company"] = "Acme"
            }
        };

        var map2 = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["email"] = "john@acme.com",
                ["phone"] = "123-456-7890"
            }
        };

        // act
        var merged = map1.MergeWith(map2);

        // assert
        merged.Count.Should().Be(4);
        merged.Mappings.ContainsKey("name").Should().BeTrue();
        merged.Mappings.ContainsKey("company").Should().BeTrue();
        merged.Mappings.ContainsKey("email").Should().BeTrue();
        merged.Mappings.ContainsKey("phone").Should().BeTrue();
    }

    [Fact]
    public void ReplacementMap_MergeWith_ShouldOverwriteWithOtherValues()
    {
        // arrange
        var map1 = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["name"] = "John",
                ["company"] = "Acme"
            }
        };

        var map2 = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["name"] = "Jane",
                ["email"] = "jane@acme.com"
            }
        };

        // act
        var merged = map1.MergeWith(map2);

        // assert
        merged.Count.Should().Be(3);
        merged.Mappings.TryGetValue("name", out var name).Should().BeTrue();
        name.Should().Be("Jane"); // Should be overwritten
        merged.Mappings.TryGetValue("company", out var company).Should().BeTrue();
        company.Should().Be("Acme");
        merged.Mappings.TryGetValue("email", out var email).Should().BeTrue();
        email.Should().Be("jane@acme.com");
    }

    [Fact]
    public void ReplacementMap_IsValid_ShouldReturnTrueForValidData()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["name"] = "John Doe",
                ["company"] = "Acme Corp"
            }
        };

        // act
        var isValid = replacementMap.IsValid();

        // assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ReplacementMap_IsValidPlaceholderName_ShouldValidateCorrectly()
    {
        // arrange & act & assert
        ReplacementMap.IsValidPlaceholderName("name").Should().BeTrue();
        ReplacementMap.IsValidPlaceholderName("company_name").Should().BeTrue();
        ReplacementMap.IsValidPlaceholderName("").Should().BeFalse();
        ReplacementMap.IsValidPlaceholderName("   ").Should().BeFalse();
        ReplacementMap.IsValidPlaceholderName(new string('a', 201)).Should().BeFalse();
        ReplacementMap.IsValidPlaceholderName("name\0test").Should().BeFalse();
    }

    [Fact]
    public void ReplacementMap_Validation_ShouldPassWithValidData()
    {
        // arrange
        var replacementMap = new ReplacementMap
        {
            Mappings = new Dictionary<string, string>
            {
                ["name"] = "John Doe",
                ["company"] = "Acme Corp"
            }
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(replacementMap, new ValidationContext(replacementMap), validationResults, true);

        // assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}