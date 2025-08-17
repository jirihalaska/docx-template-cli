using FluentAssertions;
using Moq;

namespace DocxTemplate.CLI.Tests;

public class DiscoverCommandTests
{
    [Fact]
    public void DiscoverCommand_WhenExecuted_ShouldCallTemplateDiscoveryService()
    {
        // arrange
        var mockService = new Mock<ITemplateDiscoveryService>();
        var command = new DiscoverCommand(mockService.Object);
        var options = new DiscoverCommandOptions { FolderPath = "/test/path" };
        
        // act
        var result = command.Execute(options);
        
        // assert
        result.Should().Be(0); // Success exit code
        mockService.Verify(s => s.DiscoverTemplatesAsync(options.FolderPath), Times.Once);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void DiscoverCommand_WhenFolderPathIsInvalid_ShouldReturnError(string invalidPath)
    {
        // arrange
        var mockService = new Mock<ITemplateDiscoveryService>();
        var command = new DiscoverCommand(mockService.Object);
        var options = new DiscoverCommandOptions { FolderPath = invalidPath };
        
        // act
        var result = command.Execute(options);
        
        // assert
        result.Should().Be(1); // Error exit code
        mockService.Verify(s => s.DiscoverTemplatesAsync(It.IsAny<string>()), Times.Never);
    }
}

// Sample command classes for testing (these would be in CLI project in real implementation)
public interface ITemplateDiscoveryService
{
    Task<IEnumerable<string>> DiscoverTemplatesAsync(string folderPath);
}

public class DiscoverCommand
{
    private readonly ITemplateDiscoveryService _templateService;

    public DiscoverCommand(ITemplateDiscoveryService templateService)
    {
        _templateService = templateService;
    }

    public int Execute(DiscoverCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FolderPath))
        {
            return 1; // Error
        }

        // Call the service to demonstrate the mock verification
        var _ = _templateService.DiscoverTemplatesAsync(options.FolderPath);
        return 0; // Success
    }
}

public class DiscoverCommandOptions
{
    public string FolderPath { get; set; } = string.Empty;
}
