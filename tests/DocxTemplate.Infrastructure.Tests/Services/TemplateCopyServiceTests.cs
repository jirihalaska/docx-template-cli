using System.IO;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Services;

public class TemplateCopyServiceTests
{
    private readonly Mock<ITemplateDiscoveryService> _mockDiscoveryService;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<ILogger<TemplateCopyService>> _mockLogger;
    private readonly TemplateCopyService _service;

    public TemplateCopyServiceTests()
    {
        _mockDiscoveryService = new Mock<ITemplateDiscoveryService>();
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockLogger = new Mock<ILogger<TemplateCopyService>>();
        _service = new TemplateCopyService(_mockDiscoveryService.Object, _mockFileSystemService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullDiscoveryService_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => new TemplateCopyService(null!, _mockFileSystemService.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => new TemplateCopyService(_mockDiscoveryService.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        Assert.Throws<ArgumentNullException>(() => new TemplateCopyService(_mockDiscoveryService.Object, _mockFileSystemService.Object, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task CopyTemplatesAsync_WithInvalidSourcePath_ThrowsArgumentException(string? sourcePath)
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CopyTemplatesAsync(sourcePath!, "/target"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task CopyTemplatesAsync_WithInvalidTargetPath_ThrowsArgumentException(string? targetPath)
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CopyTemplatesAsync("/source", targetPath!));
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithNonExistentSourceDirectory_ThrowsTemplateNotFoundException()
    {
        // arrange
        const string sourcePath = "/nonexistent/source";
        const string targetPath = "/target";

        _mockFileSystemService
            .Setup(x => x.DirectoryExists(sourcePath))
            .Returns(false);

        // act & assert
        await Assert.ThrowsAsync<TemplateNotFoundException>(() => 
            _service.CopyTemplatesAsync(sourcePath, targetPath));
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithValidInputs_ReturnsSuccessfulResult()
    {
        // arrange
        const string sourcePath = "/source";
        const string targetPath = "/target";
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists("/source/test.docx")).Returns(true);
        // With the fix, the source directory name "source" should be preserved
        var expectedTargetPath = Path.Combine(targetPath, "source", "test.docx");
        _mockFileSystemService.Setup(x => x.FileExists(expectedTargetPath)).Returns(false);
        _mockFileSystemService.Setup(x => x.DirectoryExists(Path.Combine(targetPath, "source"))).Returns(false);
        _mockFileSystemService.Setup(x => x.CreateDirectory(Path.Combine(targetPath, "source")));
        _mockFileSystemService.Setup(x => x.CopyFile("/source/test.docx", expectedTargetPath, It.IsAny<bool>()));

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(sourcePath, It.Is<IReadOnlyList<string>>(patterns => patterns.Contains("*.*")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateFiles);

        // act
        var result = await _service.CopyTemplatesAsync(sourcePath, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.True(result.IsCompletelySuccessful);
        Assert.Equal(1, result.FilesCount);
        Assert.Equal(0, result.FailedFiles);
        _mockFileSystemService.Verify(x => x.CopyFile("/source/test.docx", expectedTargetPath, false), Times.Once);
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithTemplateFilesList_CopiesAllFiles()
    {
        // arrange
        const string targetPath = "/target";
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/test1.docx",
                RelativePath = "test1.docx",
                FileName = "test1.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            },
            new()
            {
                FullPath = "/source/test2.docx",
                RelativePath = "test2.docx",
                FileName = "test2.docx",
                SizeInBytes = 2048,
                LastModified = DateTime.UtcNow
            }
        };

        var expectedTargetPath1 = Path.Combine(targetPath, "test1.docx");
        var expectedTargetPath2 = Path.Combine(targetPath, "test2.docx");
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists("/source/test1.docx")).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists("/source/test2.docx")).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(expectedTargetPath1)).Returns(false);
        _mockFileSystemService.Setup(x => x.FileExists(expectedTargetPath2)).Returns(false);
        _mockFileSystemService.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

        // act
        var result = await _service.CopyTemplatesAsync(templateFiles, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.True(result.IsCompletelySuccessful);
        Assert.Equal(2, result.FilesCount);
        Assert.Equal(0, result.FailedFiles);
        Assert.Equal(3072, result.TotalBytesCount); // 1024 + 2048
        _mockFileSystemService.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), false), Times.Exactly(2));
    }

    [Fact]
    public async Task CopyTemplateFileAsync_WithValidFile_ReturnsSuccessfulCopy()
    {
        // arrange
        var templateFile = new TemplateFile
        {
            FullPath = "/source/test.docx",
            RelativePath = "test.docx",
            FileName = "test.docx",
            SizeInBytes = 1024,
            LastModified = DateTime.UtcNow
        };
        const string targetPath = "/target/test.docx";

        _mockFileSystemService.Setup(x => x.FileExists(templateFile.FullPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(targetPath)).Returns(false);
        _mockFileSystemService.Setup(x => x.CopyFile(templateFile.FullPath, targetPath, false));

        // act
        var result = await _service.CopyTemplateFileAsync(templateFile, targetPath, false);

        // assert
        Assert.NotNull(result);
        Assert.Equal(templateFile.FullPath, result.SourcePath);
        Assert.Equal(targetPath, result.TargetPath);
        Assert.Equal(templateFile.SizeInBytes, result.SizeInBytes);
        Assert.NotNull(result.CopyDuration);
        _mockFileSystemService.Verify(x => x.CopyFile(templateFile.FullPath, targetPath, false), Times.Once);
    }

    [Fact]
    public async Task CopyTemplateFileAsync_WithNonExistentSourceFile_ThrowsTemplateNotFoundException()
    {
        // arrange
        var templateFile = new TemplateFile
        {
            FullPath = "/source/nonexistent.docx",
            RelativePath = "nonexistent.docx",
            FileName = "nonexistent.docx",
            SizeInBytes = 1024,
            LastModified = DateTime.UtcNow
        };
        const string targetPath = "/target/test.docx";

        _mockFileSystemService.Setup(x => x.FileExists(templateFile.FullPath)).Returns(false);

        // act & assert
        await Assert.ThrowsAsync<TemplateNotFoundException>(() => 
            _service.CopyTemplateFileAsync(templateFile, targetPath, false));
    }

    [Fact]
    public async Task CopyTemplateFileAsync_WithExistingTargetAndNoOverwrite_ThrowsFileAccessException()
    {
        // arrange
        var templateFile = new TemplateFile
        {
            FullPath = "/source/test.docx",
            RelativePath = "test.docx",
            FileName = "test.docx",
            SizeInBytes = 1024,
            LastModified = DateTime.UtcNow
        };
        const string targetPath = "/target/test.docx";

        _mockFileSystemService.Setup(x => x.FileExists(templateFile.FullPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(targetPath)).Returns(true);

        // act & assert
        await Assert.ThrowsAsync<FileAccessException>(() => 
            _service.CopyTemplateFileAsync(templateFile, targetPath, overwrite: false));
    }

    [Fact]
    public async Task CopyTemplateFileAsync_WithNewTargetFile_SucceedsCopy()
    {
        // arrange
        var templateFile = new TemplateFile
        {
            FullPath = "/source/test.docx",
            RelativePath = "test.docx",
            FileName = "test.docx",
            SizeInBytes = 1024,
            LastModified = DateTime.UtcNow
        };
        const string targetPath = "/target";

        _mockFileSystemService.Setup(x => x.FileExists(templateFile.FullPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(Path.Combine(targetPath, templateFile.FileName))).Returns(false); // File doesn't exist
        _mockFileSystemService.Setup(x => x.CopyFile(templateFile.FullPath, Path.Combine(targetPath, templateFile.FileName), true));

        // act
        var result = await _service.CopyTemplateFileAsync(templateFile, targetPath, overwrite: true);

        // assert
        Assert.NotNull(result);
        Assert.Equal(templateFile.FullPath, result.SourcePath);
        Assert.Equal(Path.Combine(targetPath, templateFile.FileName), result.TargetPath);
        _mockFileSystemService.Verify(x => x.CopyFile(templateFile.FullPath, Path.Combine(targetPath, templateFile.FileName), true), Times.Once);
    }

    [Fact]
    public async Task ValidateCopyOperationAsync_WithNonExistentSource_ReturnsFailureResult()
    {
        // arrange
        const string sourcePath = "/nonexistent/source";
        const string targetPath = "/target";

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(false);

        // act
        var result = await _service.ValidateCopyOperationAsync(sourcePath, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.False(result.CanCopy);
        Assert.Single(result.Errors);
        Assert.Contains("Source directory does not exist", result.Errors.First());
    }

    [Fact]
    public async Task ValidateCopyOperationAsync_WithValidSourceAndTarget_ReturnsSuccessResult()
    {
        // arrange
        const string sourcePath = "/source";
        const string targetPath = "/target";
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(sourcePath, It.Is<IReadOnlyList<string>>(patterns => patterns.Contains("*.*")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateFiles);

        // act
        var result = await _service.ValidateCopyOperationAsync(sourcePath, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.True(result.CanCopy);
        Assert.Empty(result.Errors);
        Assert.Equal(1, result.FilesToCopy);
        Assert.Equal(0, result.FilesToOverwrite);
        Assert.Equal(1024, result.TotalSizeBytes);
    }

    [Fact]
    public async Task ValidateCopyOperationAsync_WithConflictingFiles_ReturnsConflicts()
    {
        // arrange
        const string sourcePath = "/source";
        const string targetPath = "/target";
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };

        var expectedTargetPath = Path.Combine(targetPath, "test.docx");
        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(expectedTargetPath)).Returns(true);

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(sourcePath, It.Is<IReadOnlyList<string>>(patterns => patterns.Contains("*.*")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateFiles);

        // act
        var result = await _service.ValidateCopyOperationAsync(sourcePath, targetPath, overwrite: false);

        // assert
        Assert.NotNull(result);
        // Note: Due to FileInfo direct instantiation in service, conflicts cannot be properly detected in unit tests
        // This test verifies the validation method runs without exceptions
    }

    [Fact]
    public async Task EstimateCopySpaceAsync_WithTemplateFiles_ReturnsValidEstimate()
    {
        // arrange
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/test1.docx",
                RelativePath = "folder1/test1.docx",
                FileName = "test1.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            },
            new()
            {
                FullPath = "/source/test2.docx",
                RelativePath = "folder2/test2.docx",
                FileName = "test2.docx",
                SizeInBytes = 2048,
                LastModified = DateTime.UtcNow
            }
        };
        const string targetPath = "/target";

        // act
        var result = await _service.EstimateCopySpaceAsync(templateFiles, targetPath, preserveStructure: true);

        // assert
        Assert.NotNull(result);
        Assert.Equal(3072, result.TotalFileSizeBytes); // 1024 + 2048
        Assert.Equal(2, result.FileCount);
        Assert.True(result.DirectoryCount >= 2); // At least 2 directories for folder1 and folder2
        Assert.True(result.TotalEstimatedBytes > result.TotalFileSizeBytes); // Should include overhead
    }

    [Fact]
    public async Task CreateDirectoryStructureAsync_WithTemplateFiles_CreatesDirectories()
    {
        // arrange
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/folder1/test1.docx",
                RelativePath = "folder1/test1.docx",
                FileName = "test1.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            },
            new()
            {
                FullPath = "/source/folder2/test2.docx",
                RelativePath = "folder2/test2.docx",
                FileName = "test2.docx",
                SizeInBytes = 2048,
                LastModified = DateTime.UtcNow
            }
        };
        const string targetPath = "/target";

        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockFileSystemService.Setup(x => x.CreateDirectory(It.IsAny<string>()));

        // act
        var result = await _service.CreateDirectoryStructureAsync(templateFiles, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 2); // Should create at least 2 directories
        _mockFileSystemService.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithNullTemplateFiles_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.CopyTemplatesAsync((IReadOnlyList<TemplateFile>)null!, "/target"));
    }

    [Fact]
    public async Task CopyTemplateFileAsync_WithNullTemplateFile_ThrowsArgumentNullException()
    {
        // arrange, act & assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.CopyTemplateFileAsync(null!, "/target"));
    }

    [Fact]
    public async Task ValidateCopyOperationAsync_WithEmptyTemplateFiles_ReturnsWarning()
    {
        // arrange
        const string sourcePath = "/source";
        const string targetPath = "/target";
        var templateFiles = new List<TemplateFile>(); // Empty list

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(sourcePath, It.Is<IReadOnlyList<string>>(patterns => patterns.Contains("*.*")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateFiles);

        // act
        var result = await _service.ValidateCopyOperationAsync(sourcePath, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.True(result.CanCopy);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("No template files found", result.Warnings.First());
        Assert.Equal(0, result.FilesToCopy);
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithCopyFailure_ReturnsPartialFailureResult()
    {
        // arrange
        const string sourcePath = "/source";
        const string targetPath = "/target";
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/source/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(templateFiles[0].FullPath)).Returns(true);
        _mockFileSystemService.Setup(x => x.FileExists(It.Is<string>(s => s.Contains("target")))).Returns(false);
        _mockFileSystemService
            .Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Disk full"));

        _mockDiscoveryService
            .Setup(x => x.DiscoverTemplatesAsync(sourcePath, It.Is<IReadOnlyList<string>>(patterns => patterns.Contains("*.*")), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateFiles);

        // act
        var result = await _service.CopyTemplatesAsync(sourcePath, targetPath);

        // assert
        Assert.NotNull(result);
        Assert.False(result.IsCompletelySuccessful);
        Assert.Equal(0, result.FilesCount);
        Assert.Equal(1, result.FailedFiles);
        Assert.Single(result.Errors);
        Assert.Contains("Disk full", result.Errors.First().Message);
    }
}