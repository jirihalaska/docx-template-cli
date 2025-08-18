using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DocxTemplate.EndToEnd.Tests.RegressionTests;

/// <summary>
/// Regression tests for E2E file comparison logic to prevent timing and naming issues
/// </summary>
public class FileComparisonRegressionTests
{
    [Theory]
    [InlineData("backup_20250818_143022/file.docx", "backup_TIMESTAMP/file.docx")]
    [InlineData("backup_20241201_091530/subfolder/test.docx", "backup_TIMESTAMP/subfolder/test.docx")]
    [InlineData("backup_20230515_235959/document.docx", "backup_TIMESTAMP/document.docx")]
    [InlineData("regular_file.docx", "regular_file.docx")]
    public void NormalizeFileNameForComparison_ShouldHandleTimestampedBackups(string input, string expected)
    {
        // arrange & act - Use the same normalization logic from GuiCliIntegrationTests
        var result = NormalizeFileNameForComparison(input);
        
        // assert
        result.Should().Be(expected, 
            "Timestamped backup directories should be normalized to prevent comparison failures");
    }

    [Theory]
    [InlineData("placeholder_values.json", "values.json")]
    [InlineData("test_values.json", "values.json")]
    [InlineData("user_values.json", "values.json")]
    [InlineData("mapping.json", "mapping.json")]
    [InlineData("config.json", "config.json")]
    public void NormalizeFileNameForComparison_ShouldHandleJsonFileNaming(string input, string expected)
    {
        // arrange & act
        var result = NormalizeFileNameForComparison(input);
        
        // assert
        result.Should().Be(expected,
            "JSON files with placeholder/test/values in name should be normalized for comparison");
    }

    [Theory]
    [InlineData("subfolder/placeholder_values.json", "subfolder/values.json")]
    [InlineData("output/test_data.json", "output/values.json")]
    [InlineData("temp/user_placeholder_map.json", "temp/values.json")]
    public void NormalizeFileNameForComparison_ShouldPreserveDirectoryStructure(string input, string expected)
    {
        // arrange & act
        var result = NormalizeFileNameForComparison(input);
        
        // assert
        result.Should().Be(expected,
            "Directory structure should be preserved while normalizing file names");
    }

    [Fact]
    public void FileComparison_ShouldHandleTimingDifferencesInBackupFolders()
    {
        // arrange - Simulate GUI and CLI creating backups at different times
        var guiFiles = new[]
        {
            "backup_20250818_143022/template1.docx",
            "backup_20250818_143022/template2.docx",
            "values.json"
        };
        
        var cliFiles = new[]
        {
            "backup_20250818_143025/template1.docx", // 3 seconds later
            "backup_20250818_143025/template2.docx",
            "placeholder_values.json"
        };
        
        // act - Normalize both sets
        var normalizedGui = guiFiles.Select(NormalizeFileNameForComparison).OrderBy(f => f).ToArray();
        var normalizedCli = cliFiles.Select(NormalizeFileNameForComparison).OrderBy(f => f).ToArray();
        
        // assert - Should match after normalization
        normalizedGui.Should().BeEquivalentTo(normalizedCli,
            "File lists should match after timestamp and naming normalization");
    }

    [Fact]
    public void FileComparison_ShouldHandleJsonNamingVariations()
    {
        // arrange - Different JSON naming patterns that should be considered equivalent
        var guiFiles = new[] { "template.docx", "placeholder_values.json" };
        var cliFiles = new[] { "template.docx", "test_values.json" };
        
        // act
        var normalizedGui = guiFiles.Select(NormalizeFileNameForComparison).OrderBy(f => f).ToArray();
        var normalizedCli = cliFiles.Select(NormalizeFileNameForComparison).OrderBy(f => f).ToArray();
        
        // assert
        normalizedGui.Should().BeEquivalentTo(normalizedCli,
            "Different JSON file names with placeholder/test/values should normalize to same name");
    }

    [Fact]
    public void FileComparison_ShouldNotNormalizeUnrelatedFiles()
    {
        // arrange - Files that shouldn't be affected by normalization
        var files = new[]
        {
            "document.docx",
            "readme.txt", 
            "configuration.json",
            "data.xml"
        };
        
        // act
        var normalized = files.Select(NormalizeFileNameForComparison).ToArray();
        
        // assert - Should remain unchanged
        normalized.Should().BeEquivalentTo(files,
            "Files without timestamp patterns or specific JSON names should remain unchanged");
    }

    [Theory]
    [InlineData("backup_invalid_timestamp/file.docx")]
    [InlineData("backup_2025/file.docx")]
    [InlineData("backup_20250818/file.docx")]
    public void FileComparison_ShouldOnlyNormalizeValidTimestampPatterns(string input)
    {
        // arrange & act - Invalid timestamp patterns should not be normalized
        var result = NormalizeFileNameForComparison(input);
        
        // assert
        result.Should().Be(input,
            "Only valid backup_YYYYMMDD_HHMMSS patterns should be normalized");
    }

    [Fact]
    public void FileComparison_ComplexScenario_ShouldHandleMixedNormalization()
    {
        // arrange - Complex real-world scenario
        var guiFiles = new[]
        {
            "backup_20250818_143022/subfolder/template1.docx",
            "backup_20250818_143022/template2.docx", 
            "output/placeholder_values.json",
            "normal_file.txt"
        };
        
        var cliFiles = new[]
        {
            "backup_20250818_143155/subfolder/template1.docx", // Different timestamp
            "backup_20250818_143155/template2.docx",
            "output/test_values.json", // Different JSON name
            "normal_file.txt"
        };
        
        // act
        var normalizedGui = guiFiles.Select(NormalizeFileNameForComparison).OrderBy(f => f).ToArray();
        var normalizedCli = cliFiles.Select(NormalizeFileNameForComparison).OrderBy(f => f).ToArray();
        
        var expectedNormalized = new[]
        {
            "backup_TIMESTAMP/subfolder/template1.docx",
            "backup_TIMESTAMP/template2.docx",
            "normal_file.txt",
            "output/values.json"
        }.OrderBy(f => f).ToArray();
        
        // assert
        normalizedGui.Should().BeEquivalentTo(expectedNormalized);
        normalizedCli.Should().BeEquivalentTo(expectedNormalized);
        normalizedGui.Should().BeEquivalentTo(normalizedCli,
            "Complex file lists should normalize to identical sets");
    }

    [Fact]
    public void FileComparison_EdgeCases_ShouldHandleGracefully()
    {
        // arrange - Edge cases that could cause issues
        var edgeCases = new[]
        {
            "", // Empty string
            "backup_20250818_143022", // Directory only
            "backup_20250818_143022/", // Directory with separator
            ".json", // Just extension
            "placeholder.json", // Contains keyword but different structure
            "values", // No extension
            "/absolute/path/backup_20250818_143022/file.docx" // Absolute path
        };
        
        // act & assert - Should not throw exceptions
        foreach (var testCase in edgeCases)
        {
            Action normalize = () => NormalizeFileNameForComparison(testCase);
            normalize.Should().NotThrow($"Should handle edge case gracefully: '{testCase}'");
        }
    }

    /// <summary>
    /// Copy of the normalization logic from GuiCliIntegrationTests for regression testing
    /// </summary>
    private static string NormalizeFileNameForComparison(string filePath)
    {
        // Replace timestamped backup directories with a generic pattern
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            filePath, 
            @"backup_\d{8}_\d{6}", 
            "backup_TIMESTAMP");
            
        // Normalize JSON file names (various naming patterns become generic)
        if (normalized.EndsWith(".json"))
        {
            var fileName = Path.GetFileName(normalized);
            if (fileName.Contains("placeholder") || fileName.Contains("test") || fileName.Contains("values"))
            {
                normalized = Path.Combine(Path.GetDirectoryName(normalized) ?? "", "values.json");
            }
        }
        
        return normalized;
    }
}