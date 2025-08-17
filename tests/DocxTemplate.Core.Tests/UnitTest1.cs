using FluentAssertions;

namespace DocxTemplate.Core.Tests;

public class TemplateFileTests
{
    [Fact]
    public void TemplateFile_WhenCreatedWithValidPath_ShouldHaveCorrectProperties()
    {
        // arrange
        var filePath = "/path/to/template.docx";
        var fileName = "template.docx";
        var fileSize = 1024L;
        
        // act
        var templateFile = new TemplateFile
        {
            FilePath = filePath,
            FileName = fileName,
            FileSize = fileSize,
            LastModified = DateTime.UtcNow
        };
        
        // assert
        templateFile.FilePath.Should().Be(filePath);
        templateFile.FileName.Should().Be(fileName);
        templateFile.FileSize.Should().Be(fileSize);
        templateFile.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void PlaceholderScanResult_WhenCreated_ShouldInitializeCollections()
    {
        // arrange & act
        var result = new PlaceholderScanResult
        {
            TotalDocumentsScanned = 5,
            TotalPlaceholders = 10,
            Placeholders = new List<Placeholder>()
        };
        
        // assert
        result.TotalDocumentsScanned.Should().Be(5);
        result.TotalPlaceholders.Should().Be(10);
        result.Placeholders.Should().NotBeNull();
        result.Placeholders.Should().BeEmpty();
    }
}

// Sample domain models for testing (these would be in Core project in real implementation)
public record TemplateFile
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required DateTime LastModified { get; init; }
}

public record PlaceholderScanResult
{
    public required int TotalDocumentsScanned { get; init; }
    public required int TotalPlaceholders { get; init; }
    public required IEnumerable<Placeholder> Placeholders { get; init; }
}

public record Placeholder
{
    public required string Text { get; init; }
    public required IEnumerable<PlaceholderLocation> Locations { get; init; }
}

public record PlaceholderLocation
{
    public required string DocumentName { get; init; }
    public required int Position { get; init; }
}
