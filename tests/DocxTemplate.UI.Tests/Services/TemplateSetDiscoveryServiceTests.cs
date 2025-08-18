using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.Services;

/// <summary>
/// Unit tests for TemplateSetDiscoveryService
/// </summary>
public class TemplateSetDiscoveryServiceTests
{
    private readonly Mock<ICliCommandService> _mockCliCommandService;
    private readonly TemplateSetDiscoveryService _service;

    public TemplateSetDiscoveryServiceTests()
    {
        _mockCliCommandService = new Mock<ICliCommandService>();
        _service = new TemplateSetDiscoveryService(_mockCliCommandService.Object);
    }

    [Fact]
    public async Task DiscoverTemplateSetsAsync_WithValidResponse_ReturnsTemplateSets()
    {
        // arrange
        const string templatesPath = "./templates";
        const string jsonResponse = """
            {
                "command": "list-sets",
                "success": true,
                "data": {
                    "template_sets": [
                        {
                            "name": "Contracts",
                            "file_count": 15,
                            "total_size_formatted": "2.3 MB"
                        },
                        {
                            "name": "Reports",
                            "file_count": 8,
                            "total_size_formatted": "1.1 MB"
                        }
                    ]
                }
            }
            """;

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("list-sets", It.IsAny<string[]>()))
            .ReturnsAsync(jsonResponse);

        // act
        var result = await _service.DiscoverTemplateSetsAsync(templatesPath);

        // assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var contracts = result.First(ts => ts.Name == "Contracts");
        Assert.Equal(15, contracts.FileCount);
        Assert.Equal("2.3 MB", contracts.TotalSizeFormatted);

        var reports = result.First(ts => ts.Name == "Reports");
        Assert.Equal(8, reports.FileCount);
        Assert.Equal("1.1 MB", reports.TotalSizeFormatted);
    }

    [Fact]
    public async Task DiscoverTemplateSetsAsync_WithUnsuccessfulResponse_ReturnsEmptyList()
    {
        // arrange
        const string templatesPath = "./templates";
        const string jsonResponse = """
            {
                "command": "list-sets",
                "success": false,
                "error": "Templates folder not found"
            }
            """;

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("list-sets", It.IsAny<string[]>()))
            .ReturnsAsync(jsonResponse);

        // act
        var result = await _service.DiscoverTemplateSetsAsync(templatesPath);

        // assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DiscoverTemplateSetsAsync_WithEmptyResponse_ReturnsEmptyList()
    {
        // arrange
        const string templatesPath = "./templates";

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("list-sets", It.IsAny<string[]>()))
            .ReturnsAsync(string.Empty);

        // act
        var result = await _service.DiscoverTemplateSetsAsync(templatesPath);

        // assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DiscoverTemplateSetsAsync_WithCliException_ReturnsEmptyList()
    {
        // arrange
        const string templatesPath = "./templates";

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("list-sets", It.IsAny<string[]>()))
            .ThrowsAsync(new InvalidOperationException("CLI command failed"));

        // act
        var result = await _service.DiscoverTemplateSetsAsync(templatesPath);

        // assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DiscoverTemplateSetsAsync_WithInvalidPath_ThrowsArgumentException(string invalidPath)
    {
        // arrange
        // act & assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DiscoverTemplateSetsAsync(invalidPath));
    }

    [Fact]
    public async Task DiscoverTemplateSetsAsync_CallsCliWithCorrectArguments()
    {
        // arrange
        const string templatesPath = "./templates";
        const string jsonResponse = """
            {
                "command": "list-sets",
                "success": true,
                "data": {
                    "template_sets": []
                }
            }
            """;

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("list-sets", It.IsAny<string[]>()))
            .ReturnsAsync(jsonResponse);

        // act
        await _service.DiscoverTemplateSetsAsync(templatesPath);

        // assert
        _mockCliCommandService.Verify(x => x.ExecuteCommandAsync(
            "list-sets",
            It.Is<string[]>(args =>
                args.Length == 4 &&
                args[0] == "--templates" &&
                args[1] == templatesPath &&
                args[2] == "--format" &&
                args[3] == "json")), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullCliCommandService_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() =>
            new TemplateSetDiscoveryService(null!));
    }
}
