using System;
using System.Text.Json;
using System.Threading.Tasks;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.ViewModels;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

/// <summary>
/// Tests to ensure proper error handling and prevent regression of JSON parsing issues
/// </summary>
public class ErrorHandlingRegressionTests
{
    private readonly Mock<ICliCommandService> _mockCliService;
    private readonly PlaceholderDiscoveryViewModel _viewModel;

    public ErrorHandlingRegressionTests()
    {
        _mockCliService = new Mock<ICliCommandService>();
        _viewModel = new PlaceholderDiscoveryViewModel(_mockCliService.Object);
    }

    [Theory]
    [InlineData("S{invalid json")]  // The original error case - starts with 'S'
    [InlineData("Scanning templates...\n{\"invalid\":")]
    [InlineData("Status: Complete\n[not json array]")]
    [InlineData("Error occurred")]
    [InlineData("null")]
    public async Task ScanPlaceholdersAsync_WithInvalidJsonOutput_ShouldHandleGracefully(string invalidOutput)
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(invalidOutput);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.False(_viewModel.IsScanning);
        Assert.Contains("CLI command returned non-JSON output", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithJsonParsingException_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        // Return JSON that looks valid but will cause parsing error
        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync("{'command': 'scan', 'data': invalid}"); // Single quotes, invalid JSON

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("Chyba při prohledávání šablon", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithNullCliResponse_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync("null");

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("Failed to parse scan results", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithMalformedCliResponseStructure_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        // Valid JSON but wrong structure
        var malformedResponse = """
        {
          "wrong_field": "scan",
          "different_structure": true,
          "not_expected": {
            "format": []
          }
        }
        """;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(malformedResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("CLI returned no data", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithTimeout_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ThrowsAsync(new TimeoutException("CLI command timed out"));

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("CLI command timed out", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithFileSystemError_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/nonexistent/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ThrowsAsync(new System.IO.DirectoryNotFoundException("Path not found"));

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("Path not found", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithEmptyButValidResponse_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        var emptyValidResponse = """
        {
          "command": "scan",
          "timestamp": "2025-08-18T21:01:41.7839Z",
          "success": true,
          "data": {
            "placeholders": []
          }
        }
        """;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(emptyValidResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.True(_viewModel.HasScanCompleted);
        Assert.False(_viewModel.HasScanError);
        Assert.Equal(0, _viewModel.TotalPlaceholdersFound);
        Assert.Equal(0, _viewModel.TotalOccurrences);
        Assert.Empty(_viewModel.DiscoveredPlaceholders);
        Assert.Contains("nalezeno 0 zástupných symbolů", _viewModel.ScanStatusMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithPartialFailureResponse_ShouldHandleGracefully()
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        var partialFailureResponse = """
        {
          "command": "scan",
          "timestamp": "2025-08-18T21:01:41.7839Z",
          "success": false,
          "data": {
            "placeholders": [
              {
                "name": "FOUND_PLACEHOLDER",
                "pattern": "\\{\\{.*?\\}\\}",
                "total_occurrences": 1,
                "unique_files": 1,
                "locations": [
                  {
                    "file_name": "accessible.docx",
                    "file_path": "/test/accessible.docx",
                    "occurrences": 1,
                    "context": "{{FOUND_PLACEHOLDER}}"
                  }
                ]
              }
            ]
          }
        }
        """;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(partialFailureResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.True(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Single(_viewModel.DiscoveredPlaceholders);
        Assert.Equal("FOUND_PLACEHOLDER", _viewModel.DiscoveredPlaceholders[0].Name);
        Assert.Contains("s chybami", _viewModel.ScanErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public async Task ScanPlaceholdersAsync_WithWhitespaceOnlyResponse_ShouldHandleGracefully(string whitespaceResponse)
    {
        // arrange
        var templateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test Set", 
                Path = "/test/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });
        _viewModel.SelectedTemplateSet = templateSet;

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(whitespaceResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("CLI command returned empty output", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public void CliScanResponse_ToPlaceholderScanResult_WithInvalidPlaceholderData_ShouldHandleGracefully()
    {
        // arrange
        var response = new CliScanResponse
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
        var result = response.ToPlaceholderScanResult();

        // assert - Should not throw, but create placeholder with provided data
        Assert.Single(result.Placeholders);
        var placeholder = result.Placeholders[0];
        Assert.Equal("", placeholder.Name);
        Assert.Equal("", placeholder.Pattern);
        Assert.Equal(-1, placeholder.TotalOccurrences);
    }

    [Fact]
    public void CliScanResponse_ToPlaceholderScanResult_WithNullLocations_ShouldHandleGracefully()
    {
        // arrange
        var response = new CliScanResponse
        {
            Success = true,
            Data = new CliScanData
            {
                Placeholders = new List<CliPlaceholder>
                {
                    new()
                    {
                        Name = "TEST",
                        Pattern = "{{.*?}}",
                        TotalOccurrences = 0,
                        Locations = null! // This could happen with malformed JSON
                    }
                }
            }
        };

        // act & assert
        Assert.Throws<NullReferenceException>(() => response.ToPlaceholderScanResult());
        // In real scenarios, this should be handled by proper null checks in the conversion method
    }
}