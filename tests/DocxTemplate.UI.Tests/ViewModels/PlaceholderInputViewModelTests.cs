using System;
using System.Collections.Generic;
using System.Linq;
using DocxTemplate.Core.Models;
using DocxTemplate.UI.ViewModels;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

public class PlaceholderInputViewModelTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();

        // act & assert
        Assert.NotNull(viewModel.PlaceholderInputs);
        Assert.Empty(viewModel.PlaceholderInputs);
        Assert.Equal(0, viewModel.FilledPlaceholdersCount);
        Assert.Equal(0, viewModel.TotalPlaceholdersCount);
        Assert.NotNull(viewModel.ClearAllCommand);
        Assert.True(viewModel.ValidateStep()); // Step 3 should always be valid
    }

    [Fact]
    public void SetDiscoveredPlaceholders_WithNull_ClearsInputs()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();

        // act
        viewModel.SetDiscoveredPlaceholders(null!);

        // assert
        Assert.Empty(viewModel.PlaceholderInputs);
        Assert.Equal(0, viewModel.FilledPlaceholdersCount);
        Assert.Equal(0, viewModel.TotalPlaceholdersCount);
    }

    [Fact]
    public void SetDiscoveredPlaceholders_WithPlaceholders_CreatesInputItems()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();
        var placeholders = CreateTestPlaceholderItems();

        // act
        viewModel.SetDiscoveredPlaceholders(placeholders);

        // assert
        Assert.Equal(2, viewModel.PlaceholderInputs.Count);
        Assert.Equal(2, viewModel.TotalPlaceholdersCount);
        Assert.Equal(0, viewModel.FilledPlaceholdersCount);
        
        var firstInput = viewModel.PlaceholderInputs.First();
        Assert.Equal("NÁZEV_FIRMY", firstInput.PlaceholderName);
        Assert.Equal("NÁZEV_FIRMY:", firstInput.DisplayLabel);
        Assert.Empty(firstInput.InputValue);
        Assert.False(firstInput.IsFilled);
    }

    [Fact]
    public void CompletionSummary_ReflectsCorrectCounts()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();
        var placeholders = CreateTestPlaceholderItems();
        viewModel.SetDiscoveredPlaceholders(placeholders);

        // act - fill one placeholder
        viewModel.PlaceholderInputs.First().InputValue = "Test Company";

        // assert
        Assert.Equal(1, viewModel.FilledPlaceholdersCount);
        Assert.Equal("1 z 2 zástupných symbolů vyplněno", viewModel.CompletionSummary);
    }

    [Fact]
    public void ClearAllCommand_ClearsAllInputValues()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();
        var placeholders = CreateTestPlaceholderItems();
        viewModel.SetDiscoveredPlaceholders(placeholders);
        
        // Fill some values
        viewModel.PlaceholderInputs[0].InputValue = "Test Company";
        viewModel.PlaceholderInputs[1].InputValue = "2023-12-01";

        // act
        viewModel.ClearAllCommand.Execute().Subscribe();

        // assert
        Assert.All(viewModel.PlaceholderInputs, input => 
        {
            Assert.Empty(input.InputValue);
            Assert.False(input.IsFilled);
        });
        Assert.Equal(0, viewModel.FilledPlaceholdersCount);
    }

    [Fact]
    public void GetReplacementMapping_ReturnsOnlyFilledPlaceholders()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();
        var placeholders = CreateTestPlaceholderItems();
        viewModel.SetDiscoveredPlaceholders(placeholders);
        
        // Fill only the first placeholder
        viewModel.PlaceholderInputs[0].InputValue = "Test Company";

        // act
        var mapping = viewModel.GetReplacementMapping();

        // assert
        Assert.Single(mapping);
        Assert.True(mapping.ContainsKey("NÁZEV_FIRMY"));
        Assert.Equal("Test Company", mapping["NÁZEV_FIRMY"]);
        Assert.False(mapping.ContainsKey("DATUM_SMLOUVY"));
    }

    [Fact]
    public void GetReplacementMappingJson_ReturnsValidJson()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();
        var placeholders = CreateTestPlaceholderItems();
        viewModel.SetDiscoveredPlaceholders(placeholders);
        
        viewModel.PlaceholderInputs[0].InputValue = "Test Company";

        // act
        var json = viewModel.GetReplacementMappingJson();

        // assert
        Assert.Contains("Test Company", json);
        Assert.DoesNotContain("DATUM_SMLOUVY", json);
        // JSON may encode Czech characters as Unicode escapes
        Assert.True(json.Contains("NÁZEV_FIRMY") || json.Contains("N\\u00C1ZEV_FIRMY"));
    }

    [Fact]
    public void ValidateStep_AlwaysReturnsTrue()
    {
        // arrange
        var viewModel = new PlaceholderInputViewModel();
        var placeholders = CreateTestPlaceholderItems();
        viewModel.SetDiscoveredPlaceholders(placeholders);

        // act & assert - Should be valid with no input
        Assert.True(viewModel.ValidateStep());
        Assert.True(viewModel.IsValid);
        Assert.Empty(viewModel.ErrorMessage);

        // act & assert - Should be valid with partial input
        viewModel.PlaceholderInputs[0].InputValue = "Test";
        Assert.True(viewModel.ValidateStep());
        Assert.True(viewModel.IsValid);

        // act & assert - Should be valid with all inputs filled
        viewModel.PlaceholderInputs[1].InputValue = "2023-12-01";
        Assert.True(viewModel.ValidateStep());
        Assert.True(viewModel.IsValid);
    }

    private static List<PlaceholderItemViewModel> CreateTestPlaceholderItems()
    {
        var placeholder1 = new Placeholder
        {
            Name = "NÁZEV_FIRMY",
            Pattern = @"\{\{NÁZEV_FIRMY\}\}",
            TotalOccurrences = 2,
            Locations = new List<PlaceholderLocation>
            {
                new()
                {
                    FileName = "template1.docx",
                    FilePath = "/templates/template1.docx",
                    Occurrences = 2,
                    CharacterPositions = new[] { 100, 200 }
                }
            }
        };

        var placeholder2 = new Placeholder
        {
            Name = "DATUM_SMLOUVY",
            Pattern = @"\{\{DATUM_SMLOUVY\}\}",
            TotalOccurrences = 2,
            Locations = new List<PlaceholderLocation>
            {
                new()
                {
                    FileName = "template1.docx",
                    FilePath = "/templates/template1.docx",
                    Occurrences = 1,
                    CharacterPositions = new[] { 300 }
                },
                new()
                {
                    FileName = "template2.docx",
                    FilePath = "/templates/template2.docx",
                    Occurrences = 1,
                    CharacterPositions = new[] { 150 }
                }
            }
        };

        return new List<PlaceholderItemViewModel>
        {
            new(placeholder1),
            new(placeholder2)
        };
    }
}

public class PlaceholderInputItemViewModelTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // arrange
        var placeholder = CreateTestPlaceholder("TEST_NAME", 3);
        var placeholderItem = new PlaceholderItemViewModel(placeholder);

        // act
        var viewModel = new PlaceholderInputItemViewModel(placeholderItem);

        // assert
        Assert.Equal("TEST_NAME", viewModel.PlaceholderName);
        Assert.Equal("TEST_NAME:", viewModel.DisplayLabel);
        Assert.Equal(3, viewModel.OccurrenceCount);
        Assert.Empty(viewModel.InputValue);
        Assert.False(viewModel.IsFilled);
        Assert.True(viewModel.IsUnfilled);
    }

    [Fact]
    public void InputValue_WithWhitespace_NormalizesAutomatically()
    {
        // arrange
        var placeholder = CreateTestPlaceholder("TEST", 1);
        var placeholderItem = new PlaceholderItemViewModel(placeholder);
        var viewModel = new PlaceholderInputItemViewModel(placeholderItem);

        // act
        viewModel.InputValue = "  Test\n\tCompany  \r\n  Name  ";

        // assert
        Assert.Equal("Test Company Name", viewModel.InputValue);
        Assert.True(viewModel.IsFilled);
        Assert.False(viewModel.IsUnfilled);
    }

    [Fact]
    public void InputValue_WithEmptyOrWhitespace_RemainsUnfilled()
    {
        // arrange
        var placeholder = CreateTestPlaceholder("TEST", 1);
        var placeholderItem = new PlaceholderItemViewModel(placeholder);
        var viewModel = new PlaceholderInputItemViewModel(placeholderItem);

        // act & assert - empty string
        viewModel.InputValue = "";
        Assert.False(viewModel.IsFilled);
        Assert.True(viewModel.IsUnfilled);

        // act & assert - whitespace only
        viewModel.InputValue = "   \n\t  ";
        Assert.False(viewModel.IsFilled);
        Assert.True(viewModel.IsUnfilled);
        Assert.Empty(viewModel.InputValue); // Should be normalized to empty
    }

    [Fact]
    public void ClearValue_ResetsInputAndFilledState()
    {
        // arrange
        var placeholder = CreateTestPlaceholder("TEST", 1);
        var placeholderItem = new PlaceholderItemViewModel(placeholder);
        var viewModel = new PlaceholderInputItemViewModel(placeholderItem);
        
        viewModel.InputValue = "Some Value";
        Assert.True(viewModel.IsFilled);

        // act
        viewModel.ClearValue();

        // assert
        Assert.Empty(viewModel.InputValue);
        Assert.False(viewModel.IsFilled);
        Assert.True(viewModel.IsUnfilled);
    }

    [Theory]
    [InlineData("Simple text", "Simple text")]
    [InlineData("Text\nwith\nnewlines", "Text with newlines")]
    [InlineData("Text\twith\ttabs", "Text with tabs")]
    [InlineData("Text   with    multiple     spaces", "Text with multiple spaces")]
    [InlineData("  Leading and trailing  ", "Leading and trailing")]
    [InlineData("\n\t  Mixed\n\t whitespace  \n\t", "Mixed whitespace")]
    public void InputValue_WhitespaceNormalization_WorksCorrectly(string input, string expected)
    {
        // arrange
        var placeholder = CreateTestPlaceholder("TEST", 1);
        var placeholderItem = new PlaceholderItemViewModel(placeholder);
        var viewModel = new PlaceholderInputItemViewModel(placeholderItem);

        // act
        viewModel.InputValue = input;

        // assert
        Assert.Equal(expected, viewModel.InputValue);
        Assert.True(viewModel.IsFilled);
    }

    private static Placeholder CreateTestPlaceholder(string name, int occurrenceCount)
    {
        var locations = Enumerable.Range(0, occurrenceCount)
            .Select(i => new PlaceholderLocation
            {
                FileName = $"template{i}.docx",
                FilePath = $"/templates/template{i}.docx",
                Occurrences = 1,
                CharacterPositions = new[] { i * 100 }
            })
            .ToList();

        return new Placeholder
        {
            Name = name,
            Pattern = $@"\{{\{{{name}\}}\}}",
            TotalOccurrences = occurrenceCount,
            Locations = locations
        };
    }
}