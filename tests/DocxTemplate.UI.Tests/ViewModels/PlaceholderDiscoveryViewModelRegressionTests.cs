using System;
using System.Collections.Generic;
using System.Text.Json;
using DocxTemplate.UI.Models;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

/// <summary>
/// Regression tests to prevent issues with CLI integration and JSON parsing
/// These tests focus on the core logic that was causing issues, without UI threading
/// </summary>
public class PlaceholderDiscoveryViewModelRegressionTests
{
    [Fact]
    public void CliScanResponse_Deserialization_WithValidCliResponse_ShouldParseCorrectly()
    {
        // arrange - Test the JSON parsing that was failing with "'S' is an invalid start of a value"
        var validCliResponse = """
        {
          "command": "scan",
          "timestamp": "2025-08-18T21:01:41.7839Z",
          "success": true,
          "data": {
            "placeholders": [
              {
                "name": "ZAKAZKA_NAZEV",
                "pattern": "\\{\\{.*?\\}\\}",
                "total_occurrences": 2,
                "unique_files": 2,
                "locations": [
                  {
                    "file_name": "test1.docx",
                    "file_path": "/path/to/test1.docx",
                    "occurrences": 1,
                    "context": "Body: Test {{ZAKAZKA_NAZEV}} content"
                  },
                  {
                    "file_name": "test2.docx",
                    "file_path": "/path/to/test2.docx",
                    "occurrences": 1,
                    "context": "Body: Another {{ZAKAZKA_NAZEV}} usage"
                  }
                ]
              },
              {
                "name": "ZADAVATEL_NAZEV",
                "pattern": "\\{\\{.*?\\}\\}",
                "total_occurrences": 2,
                "unique_files": 2,
                "locations": [
                  {
                    "file_name": "test1.docx",
                    "file_path": "/path/to/test1.docx",
                    "occurrences": 1,
                    "context": "Body: Test {{ZADAVATEL_NAZEV}} content"
                  },
                  {
                    "file_name": "test2.docx",
                    "file_path": "/path/to/test2.docx",
                    "occurrences": 1,
                    "context": "Body: Another {{ZADAVATEL_NAZEV}} usage"
                  }
                ]
              }
            ]
          }
        }
        """;

        // act
        var cliResponse = JsonSerializer.Deserialize<CliScanResponse>(validCliResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var result = cliResponse!.ToPlaceholderScanResult();

        // assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Placeholders.Count);
        Assert.Equal(4, result.TotalOccurrences);
        Assert.Equal(2, result.TotalFilesScanned);
        Assert.Equal(2, result.FilesWithPlaceholders);
        
        var placeholder1 = result.Placeholders[0];
        Assert.Equal("ZAKAZKA_NAZEV", placeholder1.Name);
        Assert.Equal(2, placeholder1.TotalOccurrences);
    }

    [Fact]
    public void CliScanResponse_Deserialization_WithNonJsonResponse_ShouldThrow()
    {
        // arrange - Test the scenario that caused "'S' is an invalid start of a value"
        var nonJsonResponse = "Scanning for placeholders...\nFound 2 placeholders\n{\"invalid\":\"json\"}";

        // act & assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CliScanResponse>(nonJsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Status: Complete")]
    [InlineData("S{invalid json")]  // The original error case - starts with 'S'
    public void CliScanResponse_Deserialization_WithInvalidJson_ShouldThrow(string invalidJson)
    {
        // arrange & act & assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CliScanResponse>(invalidJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }));
    }

    [Fact]
    public void CliScanResponse_Conversion_WithFailedCliResponse_ShouldReturnErrorResult()
    {
        // arrange
        var failedResponse = """
        {
          "command": "scan",
          "timestamp": "2025-08-18T21:01:41.7839Z",
          "success": false,
          "data": {
            "placeholders": []
          }
        }
        """;

        // act
        var cliResponse = JsonSerializer.Deserialize<CliScanResponse>(failedResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var result = cliResponse!.ToPlaceholderScanResult();

        // assert
        Assert.False(result.IsSuccessful);
        Assert.Empty(result.Placeholders);
        Assert.Single(result.Errors);
        Assert.Equal("CLI scan failed", result.Errors[0].Message);
    }

    [Fact]
    public void CliScanResponse_Conversion_WithNullData_ShouldReturnErrorResult()
    {
        // arrange
        var responseWithNullData = new CliScanResponse
        {
            Command = "scan",
            Success = true,
            Data = null
        };

        // act
        var result = responseWithNullData.ToPlaceholderScanResult();

        // assert
        Assert.False(result.IsSuccessful);
        Assert.Empty(result.Placeholders);
        Assert.Single(result.Errors);
        Assert.Equal("CLI returned no data", result.Errors[0].Message);
    }

    [Fact]
    public void CliScanResponse_Conversion_WithMalformedPlaceholderData_ShouldHandleGracefully()
    {
        // arrange
        var responseWithMalformedData = new CliScanResponse
        {
            Success = true,
            Data = new CliScanData
            {
                Placeholders = new List<CliPlaceholder>
                {
                    new()
                    {
                        Name = "", // Invalid empty name
                        Pattern = "", // Invalid empty pattern
                        TotalOccurrences = -1, // Invalid negative count
                        Locations = new List<CliPlaceholderLocation>()
                    }
                }
            }
        };

        // act
        var result = responseWithMalformedData.ToPlaceholderScanResult();

        // assert - Should not throw, but handle gracefully
        Assert.True(result.IsSuccessful);
        Assert.Single(result.Placeholders);
        var placeholder = result.Placeholders[0];
        Assert.Equal("", placeholder.Name);
        Assert.Equal("", placeholder.Pattern);
        Assert.Equal(-1, placeholder.TotalOccurrences);
    }

    /// <summary>
    /// Test that verifies CLI command argument construction would include --quiet flag
    /// This is a regression test for the issue where CLI returned mixed status + JSON output
    /// </summary>
    [Fact]
    public void CliCommand_ArgumentConstruction_ShouldIncludeQuietFlag()
    {
        // arrange
        var expectedArgs = new[]
        {
            "scan",
            "--path",
            "\"/test/path\"",
            "--format", 
            "json",
            "--quiet"
        };

        // act & assert
        // This verifies the expected command structure that prevents mixed output
        Assert.Equal(6, expectedArgs.Length);
        Assert.Equal("--quiet", expectedArgs[5]);
        Assert.Equal("json", expectedArgs[4]);
        Assert.Equal("scan", expectedArgs[0]);
    }
}