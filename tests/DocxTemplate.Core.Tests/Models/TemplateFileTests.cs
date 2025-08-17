using DocxTemplate.Core.Models;
using FluentAssertions;

namespace DocxTemplate.Core.Tests.Models;

public class TemplateFileTests
{
    [Fact]
    public void TemplateFile_WithValidProperties_ShouldCreateCorrectly()
    {
        // arrange & act
        var fullPath = Path.Combine("test", "templates", "document.docx");
        var expectedDirectory = Path.Combine("test", "templates");
        
        var templateFile = new TemplateFile
        {
            FullPath = fullPath,
            RelativePath = Path.Combine("templates", "document.docx"),
            FileName = "document.docx",
            SizeInBytes = 2048,
            LastModified = new DateTime(2025, 1, 2)
        };

        // assert
        templateFile.FullPath.Should().Be(fullPath);
        templateFile.RelativePath.Should().Be(Path.Combine("templates", "document.docx"));
        templateFile.FileName.Should().Be("document.docx");
        templateFile.SizeInBytes.Should().Be(2048);
        templateFile.LastModified.Should().Be(new DateTime(2025, 1, 2));
        templateFile.DirectoryName.Should().Be(expectedDirectory);
        templateFile.FileNameWithoutExtension.Should().Be("document");
        templateFile.Extension.Should().Be(".docx");
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(512, "512 B")]
    public void DisplaySize_WithVariousSizes_ShouldReturnExpectedFormat(long sizeBytes, string expected)
    {
        // arrange
        var templateFile = new TemplateFile
        {
            FullPath = Path.Combine("test", "document.docx"),
            RelativePath = "document.docx",
            FileName = "document.docx",
            SizeInBytes = sizeBytes,
            LastModified = DateTime.UtcNow
        };

        // act
        var result = templateFile.DisplaySize;

        // assert
        result.Should().Be(expected);
    }

    [Fact]
    public void TemplateFile_WithCzechCharacters_ShouldHandleCorrectly()
    {
        // arrange & act
        var templateFile = new TemplateFile
        {
            FullPath = Path.Combine("test", "šablony", "název_dokumentu.docx"),
            RelativePath = Path.Combine("šablony", "název_dokumentu.docx"),
            FileName = "název_dokumentu.docx",
            SizeInBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // assert
        templateFile.FullPath.Should().Contain("šablony");
        templateFile.FileName.Should().Contain("název_dokumentu");
        templateFile.DirectoryName.Should().Contain("šablony");
        templateFile.FileNameWithoutExtension.Should().Be("název_dokumentu");
        templateFile.Extension.Should().Be(".docx");
    }
}