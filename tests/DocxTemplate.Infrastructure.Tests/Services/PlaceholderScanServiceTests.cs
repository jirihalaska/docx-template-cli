using System.Text.RegularExpressions;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Services;

public class PlaceholderScanServiceTests
{
    private readonly Mock<ITemplateDiscoveryService> _mockDiscoveryService;
    private readonly Mock<ILogger<PlaceholderScanService>> _mockLogger;
    private readonly PlaceholderScanService _service;

    public PlaceholderScanServiceTests()
    {
        _mockDiscoveryService = new Mock<ITemplateDiscoveryService>();
        _mockLogger = new Mock<ILogger<PlaceholderScanService>>();
        _service = new PlaceholderScanService(_mockDiscoveryService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullDiscoveryService_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => new PlaceholderScanService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => new PlaceholderScanService(_mockDiscoveryService.Object, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ScanPlaceholdersAsync_WithInvalidFolderPath_ThrowsArgumentException(string? folderPath)
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ScanPlaceholdersAsync(folderPath!));
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithInvalidPattern_ThrowsInvalidPlaceholderPatternException()
    {
        // arrange
        const string folderPath = "/valid/path";
        const string invalidPattern = "[";

        // act & assert
        await Assert.ThrowsAsync<InvalidPlaceholderPatternException>(() => 
            _service.ScanPlaceholdersAsync(folderPath, invalidPattern));
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithValidInputs_ReturnsSuccessfulResult()
    {
        // arrange
        const string folderPath = "/test/path";
        const string pattern = @"\{\{.*?\}\}";
        var templateFiles = new List<TemplateFile>(); // Empty list to avoid file access issues

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(folderPath, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateFiles);

        // act
        var result = await _service.ScanPlaceholdersAsync(folderPath, pattern, true);

        // assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.TotalFilesScanned);
        Assert.Equal(0, result.UniquePlaceholderCount);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithNullTemplateFiles_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.ScanPlaceholdersAsync((IReadOnlyList<TemplateFile>)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ScanSingleFileAsync_WithInvalidPath_ThrowsArgumentException(string? templatePath)
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ScanSingleFileAsync(templatePath!));
    }

    [Fact]
    public async Task ScanSingleFileAsync_WithNonExistentFile_ThrowsTemplateNotFoundException()
    {
        // arrange
        const string nonExistentPath = "/path/that/does/not/exist.docx";

        // act & assert
        await Assert.ThrowsAsync<TemplateNotFoundException>(() => 
            _service.ScanSingleFileAsync(nonExistentPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidatePattern_WithInvalidPattern_ThrowsInvalidPlaceholderPatternException(string? pattern)
    {
        // arrange, act & assert
        Assert.Throws<InvalidPlaceholderPatternException>(() => _service.ValidatePattern(pattern!));
    }

    [Fact]
    public void ValidatePattern_WithValidPattern_ReturnsTrue()
    {
        // arrange
        const string validPattern = @"\{\{.*?\}\}";

        // act
        var result = _service.ValidatePattern(validPattern);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePattern_WithInvalidRegexPattern_ThrowsInvalidPlaceholderPatternException()
    {
        // arrange
        const string invalidPattern = "[";

        // act & assert
        Assert.Throws<InvalidPlaceholderPatternException>(() => _service.ValidatePattern(invalidPattern));
    }

    [Theory]
    [InlineData("", "{{.*?}}")]
    [InlineData(null, "{{.*?}}")]
    public void ExtractPlaceholderNames_WithEmptyContent_ReturnsEmptyList(string? content, string pattern)
    {
        // arrange, act
        var result = _service.ExtractPlaceholderNames(content!, pattern);

        // assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractPlaceholderNames_WithValidContent_ReturnsPlaceholderNames()
    {
        // arrange
        const string content = "Hello {{name}}, your order {{orderId}} is ready. Total: {{total}}";
        const string pattern = @"\{\{.*?\}\}";

        // act
        var result = _service.ExtractPlaceholderNames(content, pattern);

        // assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("name", result);
        Assert.Contains("orderId", result);
        Assert.Contains("total", result);
    }

    [Fact]
    public void ExtractPlaceholderNames_WithDuplicatePlaceholders_ReturnsUniqueNames()
    {
        // arrange
        const string content = "Hello {{name}}, Mr. {{name}}!";
        const string pattern = @"\{\{.*?\}\}";

        // act
        var result = _service.ExtractPlaceholderNames(content, pattern);

        // assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("name", result);
    }

    [Fact]
    public void ExtractPlaceholderNames_WithComplexPattern_ReturnsCorrectNames()
    {
        // arrange
        const string content = "Value: ${value}, Another: [field], Third: {{name}}";
        const string pattern = @"\$\{.*?\}|\[.*?\]|\{\{.*?\}\}";

        // act
        var result = _service.ExtractPlaceholderNames(content, pattern);

        // assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("${value", result); // The method trims {, }, [, ], <, > but not $
        Assert.Contains("field", result);
        Assert.Contains("name", result);
    }

    [Fact]
    public void GetPlaceholderStatistics_WithNullScanResult_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => _service.GetPlaceholderStatistics(null!));
    }

    [Fact]
    public void GetPlaceholderStatistics_WithValidScanResult_ReturnsStatistics()
    {
        // arrange
        var placeholders = new List<Placeholder>
        {
            new()
            {
                Name = "name",
                Pattern = "{{.*?}}",
                TotalOccurrences = 5,
                Locations = new List<PlaceholderLocation>
                {
                    new()
                    {
                        FileName = "test1.docx",
                        FilePath = "/path/test1.docx",
                        Occurrences = 3,
                        Context = "Hello {{name}}"
                    },
                    new()
                    {
                        FileName = "test2.docx",
                        FilePath = "/path/test2.docx",
                        Occurrences = 2,
                        Context = "Dear {{name}}"
                    }
                }.AsReadOnly()
            }
        };

        var scanResult = PlaceholderScanResult.Success(placeholders, 2, TimeSpan.FromMilliseconds(100), 2);

        // act
        var statistics = _service.GetPlaceholderStatistics(scanResult);

        // assert
        Assert.NotNull(statistics);
        Assert.Equal(1, statistics.TotalUniquePlaceholders);
        Assert.Equal(5, statistics.TotalOccurrences);
        Assert.Equal(2, statistics.FilesScanned);
        Assert.Equal(2, statistics.FilesWithPlaceholders);
        Assert.Equal(TimeSpan.FromMilliseconds(100), statistics.ScanDuration);
        Assert.Single(statistics.MostCommonPlaceholders);
        Assert.Equal("name", statistics.MostCommonPlaceholders.First().Name);
    }

    [Fact]
    public void GetPlaceholderStatistics_WithEmptyResult_ReturnsZeroStatistics()
    {
        // arrange
        var scanResult = PlaceholderScanResult.Success(new List<Placeholder>(), 1, TimeSpan.FromMilliseconds(50), 0);

        // act
        var statistics = _service.GetPlaceholderStatistics(scanResult);

        // assert
        Assert.NotNull(statistics);
        Assert.Equal(0, statistics.TotalUniquePlaceholders);
        Assert.Equal(0, statistics.TotalOccurrences);
        Assert.Equal(1, statistics.FilesScanned);
        Assert.Equal(0, statistics.FilesWithPlaceholders);
        Assert.Empty(statistics.MostCommonPlaceholders);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithDiscoveryServiceException_ReturnsErrorResult()
    {
        // arrange
        const string folderPath = "/test/path";
        const string pattern = @"\{\{.*?\}\}";
        var exception = new DirectoryNotFoundException("Directory not found");

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(folderPath, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // act
        var result = await _service.ScanPlaceholdersAsync(folderPath, pattern, true);

        // assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccessful);
        Assert.Equal(0, result.TotalFilesScanned);
        Assert.Single(result.Errors);
        Assert.Equal(folderPath, result.Errors.First().FilePath);
        Assert.Contains("Directory not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task ScanPlaceholdersAsync_WithCancellation_ReturnsErrorResult()
    {
        // arrange
        const string folderPath = "/test/path";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(folderPath, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Test cancellation"));

        // act
        var result = await _service.ScanPlaceholdersAsync(folderPath, cancellationToken: cancellationTokenSource.Token);

        // assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccessful);
        Assert.Single(result.Errors);
        Assert.Contains("Test cancellation", result.Errors.First().Message);
    }
}