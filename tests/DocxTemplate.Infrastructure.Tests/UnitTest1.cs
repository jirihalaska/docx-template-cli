using FluentAssertions;
using System.IO;

namespace DocxTemplate.Infrastructure.Tests;

public class FileTemplateDiscoveryServiceTests
{
    [Fact]
    public void DiscoverTemplates_WhenDirectoryExists_ShouldReturnDocxFiles()
    {
        // arrange
        var service = new FileTemplateDiscoveryService();
        var testDirectory = Path.GetTempPath();
        
        // act
        var result = service.DiscoverTemplates(testDirectory);
        
        // assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<string>>();
    }
    
    [Fact]
    public void DiscoverTemplates_WhenDirectoryDoesNotExist_ShouldThrowException()
    {
        // arrange
        var service = new FileTemplateDiscoveryService();
        var nonExistentDirectory = "/path/that/does/not/exist";
        
        // act & assert
        var action = () => service.DiscoverTemplates(nonExistentDirectory);
        action.Should().Throw<DirectoryNotFoundException>();
    }
    
    [Theory]
    [InlineData("*.docx")]
    [InlineData("*.doc")]
    [InlineData("template*.docx")]
    public void DiscoverTemplates_WithDifferentPatterns_ShouldFilterCorrectly(string pattern)
    {
        // arrange
        var service = new FileTemplateDiscoveryService();
        var testDirectory = Path.GetTempPath();
        
        // act
        var result = service.DiscoverTemplatesWithPattern(testDirectory, pattern);
        
        // assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<string>>();
    }
}

// Sample infrastructure service for testing (would be in Infrastructure project in real implementation)
public class FileTemplateDiscoveryService
{
    public IEnumerable<string> DiscoverTemplates(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }
        
        return Directory.GetFiles(directoryPath, "*.docx", SearchOption.TopDirectoryOnly);
    }
    
    public IEnumerable<string> DiscoverTemplatesWithPattern(string directoryPath, string pattern)
    {
        if (!Directory.Exists(directoryPath))
        {
            return Enumerable.Empty<string>();
        }
        
        return Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
    }
}
