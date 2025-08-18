using System;
using System.Collections.Generic;
using System.Text.Json;
using DocxTemplate.UI.Models;
using Xunit;

namespace DocxTemplate.UI.Tests.Models;

public class CliScanResponseTests
{
    [Fact]
    public void DeserializeCliResponse_ValidJson_ShouldParseCorrectly()
    {
        // arrange
        var json = """
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
              }
            ]
          }
        }
        """;

        // act
        var response = JsonSerializer.Deserialize<CliScanResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // assert
        Assert.NotNull(response);
        Assert.Equal("scan", response.Command);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data.Placeholders);
        
        var placeholder = response.Data.Placeholders[0];
        Assert.Equal("ZAKAZKA_NAZEV", placeholder.Name);
        Assert.Equal("\\{\\{.*?\\}\\}", placeholder.Pattern);
        Assert.Equal(2, placeholder.TotalOccurrences);
        Assert.Equal(2, placeholder.UniqueFiles);
        Assert.Equal(2, placeholder.Locations.Count);
        
        var location1 = placeholder.Locations[0];
        Assert.Equal("test1.docx", location1.FileName);
        Assert.Equal("/path/to/test1.docx", location1.FilePath);
        Assert.Equal(1, location1.Occurrences);
        Assert.Equal("Body: Test {{ZAKAZKA_NAZEV}} content", location1.Context);
    }

    [Fact]
    public void ToPlaceholderScanResult_ValidResponse_ShouldConvertCorrectly()
    {
        // arrange
        var response = new CliScanResponse
        {
            Command = "scan",
            Success = true,
            Data = new CliScanData
            {
                Placeholders = new List<CliPlaceholder>
                {
                    new()
                    {
                        Name = "TEST_PLACEHOLDER",
                        Pattern = "\\{\\{.*?\\}\\}",
                        TotalOccurrences = 3,
                        UniqueFiles = 2,
                        Locations = new List<CliPlaceholderLocation>
                        {
                            new()
                            {
                                FileName = "file1.docx",
                                FilePath = "/path/to/file1.docx",
                                Occurrences = 2,
                                Context = "Test context 1"
                            },
                            new()
                            {
                                FileName = "file2.docx", 
                                FilePath = "/path/to/file2.docx",
                                Occurrences = 1,
                                Context = "Test context 2"
                            }
                        }
                    }
                }
            }
        };

        // act
        var result = response.ToPlaceholderScanResult();

        // assert
        Assert.True(result.IsSuccessful);
        Assert.Single(result.Placeholders);
        Assert.Equal(2, result.TotalFilesScanned);
        Assert.Equal(1, result.FilesWithPlaceholders);
        Assert.Equal(3, result.TotalOccurrences);
        Assert.Equal(0, result.FailedFiles);
        
        var placeholder = result.Placeholders[0];
        Assert.Equal("TEST_PLACEHOLDER", placeholder.Name);
        Assert.Equal("\\{\\{.*?\\}\\}", placeholder.Pattern);
        Assert.Equal(3, placeholder.TotalOccurrences);
        Assert.Equal(2, placeholder.Locations.Count);
        
        var location1 = placeholder.Locations[0];
        Assert.Equal("file1.docx", location1.FileName);
        Assert.Equal("/path/to/file1.docx", location1.FilePath);
        Assert.Equal(2, location1.Occurrences);
        Assert.Equal("Test context 1", location1.Context);
    }

    [Fact]
    public void ToPlaceholderScanResult_NullData_ShouldReturnErrorResult()
    {
        // arrange
        var response = new CliScanResponse
        {
            Command = "scan",
            Success = false,
            Data = null
        };

        // act
        var result = response.ToPlaceholderScanResult();

        // assert
        Assert.False(result.IsSuccessful);
        Assert.Empty(result.Placeholders);
        Assert.Equal(0, result.TotalFilesScanned);
        Assert.Equal(0, result.FilesWithPlaceholders);
        Assert.Equal(0, result.TotalOccurrences);
        Assert.Equal(1, result.FailedFiles);
        Assert.Single(result.Errors);
        Assert.Equal("CLI returned no data", result.Errors[0].Message);
    }

    [Fact]
    public void ToPlaceholderScanResult_FailedResponse_ShouldReturnErrorResult()
    {
        // arrange
        var response = new CliScanResponse
        {
            Command = "scan",
            Success = false,
            Data = new CliScanData
            {
                Placeholders = new List<CliPlaceholder>()
            }
        };

        // act
        var result = response.ToPlaceholderScanResult();

        // assert
        Assert.False(result.IsSuccessful);
        Assert.Empty(result.Placeholders);
        Assert.Equal(0, result.TotalFilesScanned);
        Assert.Equal(0, result.FilesWithPlaceholders);
        Assert.Equal(0, result.TotalOccurrences);
        Assert.Single(result.Errors);
        Assert.Equal("CLI scan failed", result.Errors[0].Message);
    }

    [Fact]
    public void ToPlaceholderScanResult_EmptyPlaceholders_ShouldHandleCorrectly()
    {
        // arrange
        var response = new CliScanResponse
        {
            Command = "scan",
            Success = true,
            Data = new CliScanData
            {
                Placeholders = new List<CliPlaceholder>()
            }
        };

        // act
        var result = response.ToPlaceholderScanResult();

        // assert
        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Placeholders);
        Assert.Equal(0, result.TotalFilesScanned);
        Assert.Equal(0, result.FilesWithPlaceholders);
        Assert.Equal(0, result.TotalOccurrences);
        Assert.Equal(0, result.FailedFiles);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DeserializeCliResponse_MalformedJson_ShouldThrow()
    {
        // arrange
        var malformedJson = """
        {
          "command": "scan",
          "success": true,
          "data": {
            "placeholders": [
              {
                "name": "TEST"
                // Missing comma and closing
        """;

        // act & assert
        Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<CliScanResponse>(malformedJson));
    }

    [Fact]
    public void DeserializeCliResponse_MissingProperties_ShouldUseDefaults()
    {
        // arrange - JSON missing some optional properties
        var json = """
        {
          "command": "scan",
          "success": true,
          "data": {
            "placeholders": [
              {
                "name": "TEST_PLACEHOLDER",
                "total_occurrences": 1,
                "locations": []
              }
            ]
          }
        }
        """;

        // act
        var response = JsonSerializer.Deserialize<CliScanResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // assert
        Assert.NotNull(response);
        Assert.Single(response.Data!.Placeholders);
        
        var placeholder = response.Data.Placeholders[0];
        Assert.Equal("TEST_PLACEHOLDER", placeholder.Name);
        Assert.Equal(string.Empty, placeholder.Pattern); // Default value
        Assert.Equal(1, placeholder.TotalOccurrences);
        Assert.Equal(0, placeholder.UniqueFiles); // Default value
        Assert.Empty(placeholder.Locations);
    }
}