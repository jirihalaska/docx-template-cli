using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Services;

public class TemplateDiscoveryServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<ILogger<TemplateDiscoveryService>> _mockLogger;
    private readonly TemplateDiscoveryService _service;

    public TemplateDiscoveryServiceTests()
    {
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockLogger = new Mock<ILogger<TemplateDiscoveryService>>();
        _service = new TemplateDiscoveryService(_mockFileSystemService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task DiscoverTemplatesAsync_WithValidPath_ReturnsTemplates()
    {
        // arrange
        var folderPath = "/templates";
        _mockFileSystemService.Setup(x => x.DirectoryExists(folderPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.EnumerateFiles(folderPath, "*.docx", SearchOption.AllDirectories))
            .Returns(["/templates/doc1.docx", "/templates/doc2.docx", "/templates/~$temp.docx"]);

        _mockFileSystemService.Setup(x => x.FileExists("/templates/doc1.docx")).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists("/templates/doc2.docx")).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFullPath("/templates/doc1.docx")).Returns("/templates/doc1.docx");
        _mockFileSystemService.Setup(x => x.GetFullPath("/templates/doc2.docx")).Returns("/templates/doc2.docx");
        _mockFileSystemService.Setup(x => x.GetFullPath(folderPath)).Returns(folderPath);
        _mockFileSystemService.Setup(x => x.GetFileName("/templates/doc1.docx")).Returns("doc1.docx");
        _mockFileSystemService.Setup(x => x.GetFileName("/templates/doc2.docx")).Returns("doc2.docx");
        _mockFileSystemService.Setup(x => x.GetFileSize("/templates/doc1.docx")).Returns(1024);
        _mockFileSystemService.Setup(x => x.GetFileSize("/templates/doc2.docx")).Returns(2048);
        _mockFileSystemService.Setup(x => x.GetLastWriteTime("/templates/doc1.docx")).Returns(DateTime.UtcNow);
        _mockFileSystemService.Setup(x => x.GetLastWriteTime("/templates/doc2.docx")).Returns(DateTime.UtcNow);

        // act
        var result = await _service.DiscoverTemplatesAsync(folderPath, true);

        // assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should exclude the temporary file
        Assert.All(result, t => Assert.DoesNotContain("~$", t.FileName));
    }

    [Fact]
    public async Task DiscoverTemplatesAsync_WithNonExistentPath_ThrowsTemplateNotFoundException()
    {
        // arrange
        var folderPath = "/non-existent";
        _mockFileSystemService.Setup(x => x.DirectoryExists(folderPath)).Returns(false);

        // act & assert
        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _service.DiscoverTemplatesAsync(folderPath, true));
    }

    [Fact]
    public async Task DiscoverTemplatesAsync_WithPatterns_FiltersCorrectly()
    {
        // arrange
        var folderPath = "/templates";
        _mockFileSystemService.Setup(x => x.DirectoryExists(folderPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.EnumerateFiles(folderPath, "template*.docx", SearchOption.AllDirectories))
            .Returns(["/templates/template1.docx", "/templates/template2.docx"]);

        SetupTemplateFile("/templates/template1.docx", folderPath, "template1.docx", 1024);
        SetupTemplateFile("/templates/template2.docx", folderPath, "template2.docx", 2048);

        // act
        var result = await _service.DiscoverTemplatesAsync(
            folderPath,
            ["template*.docx"], 
            true);

        // assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.StartsWith("template", t.FileName));
    }

    [Fact]
    public async Task DiscoverTemplatesAsync_NonRecursive_OnlySearchesTopLevel()
    {
        // arrange
        var folderPath = "/templates";
        _mockFileSystemService.Setup(x => x.DirectoryExists(folderPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.EnumerateFiles(folderPath, "*.docx", SearchOption.TopDirectoryOnly))
            .Returns(["/templates/doc1.docx"]);

        SetupTemplateFile("/templates/doc1.docx", folderPath, "doc1.docx", 1024);

        // act
        var result = await _service.DiscoverTemplatesAsync(folderPath, false);

        // assert
        Assert.NotNull(result);
        Assert.Single(result);
        _mockFileSystemService.Verify(x => x.EnumerateFiles(folderPath, "*.docx", SearchOption.TopDirectoryOnly), Times.Once);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithValidDocxFile_ReturnsTrue()
    {
        // arrange
        var templatePath = "/templates/valid.docx";
        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFileSize(templatePath)).Returns(1024);

        // act
        var result = await _service.ValidateTemplateAsync(templatePath);

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithNonDocxFile_ReturnsFalse()
    {
        // arrange
        var templatePath = "/templates/file.txt";
        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(true);

        // act
        var result = await _service.ValidateTemplateAsync(templatePath);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithTemporaryFile_ReturnsFalse()
    {
        // arrange
        var templatePath = "/templates/~$temp.docx";
        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(true);

        // act
        var result = await _service.ValidateTemplateAsync(templatePath);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithNonExistentFile_ReturnsFalse()
    {
        // arrange
        var templatePath = "/templates/missing.docx";
        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(false);

        // act
        var result = await _service.ValidateTemplateAsync(templatePath);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetTemplateMetadataAsync_WithValidFile_ReturnsMetadata()
    {
        // arrange
        var templatePath = "/templates/doc.docx";
        var basePath = "/templates";
        var lastModified = DateTime.UtcNow;

        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFullPath(templatePath)).Returns(templatePath);
        _mockFileSystemService.Setup(x => x.GetFullPath(basePath)).Returns(basePath);
        _mockFileSystemService.Setup(x => x.GetFileName(templatePath)).Returns("doc.docx");
        _mockFileSystemService.Setup(x => x.GetFileSize(templatePath)).Returns(2048);
        _mockFileSystemService.Setup(x => x.GetLastWriteTime(templatePath)).Returns(lastModified);

        // act
        var result = await _service.GetTemplateMetadataAsync(templatePath, basePath);

        // assert
        Assert.NotNull(result);
        Assert.Equal(templatePath, result.FullPath);
        Assert.Equal("doc.docx", result.FileName);
        Assert.Equal(2048, result.SizeInBytes);
        Assert.Equal(lastModified.ToUniversalTime(), result.LastModified);
    }

    [Fact]
    public async Task GetTemplateMetadataAsync_WithNonExistentFile_ThrowsTemplateNotFoundException()
    {
        // arrange
        var templatePath = "/templates/missing.docx";
        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(false);

        // act & assert
        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _service.GetTemplateMetadataAsync(templatePath));
    }

    [Fact]
    public async Task GetTemplateMetadataAsync_WithoutBasePath_UsesFileName()
    {
        // arrange
        var templatePath = "/templates/sub/doc.docx";

        _mockFileSystemService.Setup(x => x.FileExists(templatePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFullPath(templatePath)).Returns(templatePath);
        _mockFileSystemService.Setup(x => x.GetFileName(templatePath)).Returns("doc.docx");
        _mockFileSystemService.Setup(x => x.GetFileSize(templatePath)).Returns(1024);
        _mockFileSystemService.Setup(x => x.GetLastWriteTime(templatePath)).Returns(DateTime.UtcNow);

        // act
        var result = await _service.GetTemplateMetadataAsync(templatePath);

        // assert
        Assert.NotNull(result);
        Assert.Equal("doc.docx", result.RelativePath);
    }

    private void SetupTemplateFile(string fullPath, string basePath, string fileName, long size)
    {
        _mockFileSystemService.Setup(x => x.FileExists(fullPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.GetFullPath(fullPath)).Returns(fullPath);
        _mockFileSystemService.Setup(x => x.GetFullPath(basePath)).Returns(basePath);
        _mockFileSystemService.Setup(x => x.GetFileName(fullPath)).Returns(fileName);
        _mockFileSystemService.Setup(x => x.GetFileSize(fullPath)).Returns(size);
        _mockFileSystemService.Setup(x => x.GetLastWriteTime(fullPath)).Returns(DateTime.UtcNow);
    }
}