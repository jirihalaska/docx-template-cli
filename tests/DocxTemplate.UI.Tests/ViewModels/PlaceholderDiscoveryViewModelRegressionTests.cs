using System;
using System.Threading.Tasks;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.ViewModels;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.ViewModels;

/// <summary>
/// Regression tests to prevent issues with CLI integration and JSON parsing
/// </summary>
public class PlaceholderDiscoveryViewModelRegressionTests
{
    private readonly Mock<ICliCommandService> _mockCliService;
    private readonly PlaceholderDiscoveryViewModel _viewModel;

    public PlaceholderDiscoveryViewModelRegressionTests()
    {
        _mockCliService = new Mock<ICliCommandService>();
        _viewModel = new PlaceholderDiscoveryViewModel(_mockCliService.Object);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_ShouldUseQuietFlag()
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
            .Setup(x => x.ExecuteCommandAsync("", It.IsAny<string[]>()))
            .ReturnsAsync(ValidCliJsonResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        _mockCliService.Verify(
            x => x.ExecuteCommandAsync("", 
                It.Is<string[]>(args => 
                    args.Length == 6 &&
                    args[0] == "scan" &&
                    args[1] == "--path" &&
                    args[2] == "\"/test/path\"" &&
                    args[3] == "--format" &&
                    args[4] == "json" &&
                    args[5] == "--quiet")),
            Times.Once);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithValidCliResponse_ShouldParseCorrectly()
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
            .ReturnsAsync(ValidCliJsonResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.True(_viewModel.HasScanCompleted);
        Assert.False(_viewModel.HasScanError);
        Assert.False(_viewModel.IsScanning);
        Assert.Equal(2, _viewModel.TotalPlaceholdersFound);
        Assert.Equal(4, _viewModel.TotalOccurrences);
        Assert.Equal(2, _viewModel.DiscoveredPlaceholders.Count);
        
        var placeholder1 = _viewModel.DiscoveredPlaceholders[0];
        Assert.Equal("ZAKAZKA_NAZEV", placeholder1.Name);
        Assert.Equal(2, placeholder1.OccurrenceCount);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithNonJsonResponse_ShouldHandleError()
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

        // Simulate old behavior that returned status messages + JSON
        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync("Scanning for placeholders...\nFound 2 placeholders\n{\"invalid\":\"json\"}");

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.False(_viewModel.IsScanning);
        Assert.Contains("CLI command returned non-JSON output", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithEmptyResponse_ShouldHandleError()
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
            .ReturnsAsync("");

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("CLI command returned empty output", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithCliException_ShouldHandleError()
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
            .ThrowsAsync(new InvalidOperationException("CLI execution failed"));

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("Chyba při prohledávání šablon: CLI execution failed", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithoutSelectedTemplateSet_ShouldHandleError()
    {
        // arrange
        _viewModel.SelectedTemplateSet = null;

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.False(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("Není vybrána žádná sada šablon", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithFailedCliResponse_ShouldHandleError()
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

        _mockCliService
            .Setup(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(failedResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();

        // assert
        Assert.True(_viewModel.HasScanCompleted);
        Assert.True(_viewModel.HasScanError);
        Assert.Contains("Prohledávání dokončeno s chybami", _viewModel.ScanErrorMessage);
    }

    [Fact]
    public void ValidateStep_WithNoScanCompleted_ShouldReturnFalse()
    {
        // arrange
        _viewModel.SelectedTemplateSet = new TemplateSetItemViewModel(
            new TemplateSetInfo 
            { 
                Name = "Test", 
                Path = "/path", 
                FileCount = 1, 
                TotalSize = 100, 
                TotalSizeFormatted = "100 B", 
                LastModified = DateTime.UtcNow 
            });

        // act
        var isValid = _viewModel.ValidateStep();

        // assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateStep_AfterSuccessfulScan_ShouldReturnTrue()
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
            .ReturnsAsync(ValidCliJsonResponse);

        // act
        await _viewModel.ScanPlaceholdersAsync();
        var isValid = _viewModel.ValidateStep();

        // assert
        Assert.True(isValid);
    }

    private const string ValidCliJsonResponse = """
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
}