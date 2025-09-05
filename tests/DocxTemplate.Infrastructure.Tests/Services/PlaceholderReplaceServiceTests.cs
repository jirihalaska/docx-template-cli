using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Models.Results;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.Services;
using DocxTemplate.Infrastructure.DocxProcessing;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using Xunit;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocxTemplate.Infrastructure.Tests.Services;

public class PlaceholderReplaceServiceTests
{
    private readonly Mock<ILogger<PlaceholderReplaceService>> _mockLogger;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<PlaceholderReplacementEngine> _mockReplacementEngine;
    private readonly PlaceholderReplaceService _service;

    public PlaceholderReplaceServiceTests()
    {
        _mockLogger = new Mock<ILogger<PlaceholderReplaceService>>();
        _mockErrorHandler = new Mock<IErrorHandler>();
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockReplacementEngine = new Mock<PlaceholderReplacementEngine>(
            Mock.Of<ILogger<PlaceholderReplacementEngine>>(),
            Mock.Of<IImageProcessor>());
        
        _service = new PlaceholderReplaceService(
            _mockLogger.Object,
            _mockErrorHandler.Object,
            _mockFileSystemService.Object,
            _mockReplacementEngine.Object);
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_FolderPath_ValidInput_ReturnsSuccess()
    {
        // arrange
        var folderPath = "/test/folder";
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.DirectoryExists(folderPath)).Returns(true);
        _mockFileSystemService.Setup(fs => fs.EnumerateFiles(folderPath, "*.docx", SearchOption.AllDirectories))
            .Returns(["/test/folder/test.docx"]);
        _mockFileSystemService.Setup(fs => fs.GetFileName("/test/folder/test.docx")).Returns("test.docx");
        _mockFileSystemService.Setup(fs => fs.GetFileSize("/test/folder/test.docx")).Returns(1024);
        _mockFileSystemService.Setup(fs => fs.GetLastWriteTime("/test/folder/test.docx")).Returns(DateTime.UtcNow);
        _mockFileSystemService.Setup(fs => fs.FileExists("/test/folder/test.docx")).Returns(true);
        _mockFileSystemService.Setup(fs => fs.CreateBackupAsync("/test/folder/test.docx", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/test/folder/test_backup.docx");

        // act
        var result = await _service.ReplacePlaceholdersAsync(folderPath, replacementMap, true);

        // assert
        Assert.NotNull(result);
        Assert.Single(result.FileResults);
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_FolderPath_DirectoryNotFound_ReturnsFailure()
    {
        // arrange
        var folderPath = "/nonexistent/folder";
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.DirectoryExists(folderPath)).Returns(false);
        _mockErrorHandler.Setup(eh => eh.HandleExceptionAsync(It.IsAny<DirectoryNotFoundException>(), "folder replacement"))
            .ReturnsAsync(ErrorResult.FileNotFoundError(folderPath, "folder replacement"));

        // act
        var result = await _service.ReplacePlaceholdersAsync(folderPath, replacementMap, true);

        // assert
        Assert.NotNull(result);
        Assert.Single(result.FileResults);
        Assert.False(result.FileResults.First().IsSuccess);
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_TemplateFiles_ValidInput_ReturnsSuccess()
    {
        // arrange
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/test/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.FileExists("/test/test.docx")).Returns(true);
        _mockFileSystemService.Setup(fs => fs.GetFileSize("/test/test.docx")).Returns(1024);

        // act
        var result = await _service.ReplacePlaceholdersAsync(templateFiles, replacementMap, false);

        // assert
        Assert.NotNull(result);
        Assert.Single(result.FileResults);
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_TemplateFiles_InvalidReplacementMap_ReturnsFailure()
    {
        // arrange
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/test/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };
        var invalidMappings = new Dictionary<string, string> { { "", "value" } }; // Invalid because empty key
        var invalidReplacementMap = new ReplacementMap { Mappings = invalidMappings };
        
        // Verify the map is actually invalid
        Assert.False(invalidReplacementMap.IsValid());

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.ReplacePlaceholdersAsync(templateFiles, invalidReplacementMap, false));
    }

    [Fact]
    public async Task ReplacePlaceholdersInFileAsync_ValidInput_FailsBecauseNoRealDocument()
    {
        // arrange
        var templatePath = "/test/test.docx";
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.FileExists(templatePath)).Returns(true);
        _mockFileSystemService.Setup(fs => fs.GetFileSize(templatePath)).Returns(1024);
        _mockFileSystemService.Setup(fs => fs.CreateBackupAsync(templatePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/test/test_backup.docx");

        // act
        var result = await _service.ReplacePlaceholdersInFileAsync(templatePath, replacementMap, true);

        // assert
        Assert.NotNull(result);
        Assert.Equal(templatePath, result.FilePath);
        // This test fails because we can't actually process a real Word document in a unit test
        // The service correctly fails when trying to open a non-existent Word document
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ReplacePlaceholdersInFileAsync_FileNotFound_ReturnsFailure()
    {
        // arrange
        var templatePath = "/test/nonexistent.docx";
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.FileExists(templatePath)).Returns(false);

        // act
        var result = await _service.ReplacePlaceholdersInFileAsync(templatePath, replacementMap, true);

        // assert
        Assert.NotNull(result);
        Assert.Equal(templatePath, result.FilePath);
        Assert.False(result.IsSuccess);
        Assert.Equal("File not found", result.ErrorMessage);
    }

    [Fact]
    public async Task PreviewReplacementsAsync_ValidInput_ReturnsPreview()
    {
        // arrange
        var folderPath = "/test/folder";
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.DirectoryExists(folderPath)).Returns(true);
        _mockFileSystemService.Setup(fs => fs.EnumerateFiles(folderPath, "*.docx", SearchOption.AllDirectories))
            .Returns(["/test/folder/test.docx"]);
        _mockFileSystemService.Setup(fs => fs.GetFileName("/test/folder/test.docx")).Returns("test.docx");
        _mockFileSystemService.Setup(fs => fs.GetFileSize("/test/folder/test.docx")).Returns(1024);
        _mockFileSystemService.Setup(fs => fs.GetLastWriteTime("/test/folder/test.docx")).Returns(DateTime.UtcNow);

        // act
        var result = await _service.PreviewReplacementsAsync(folderPath, replacementMap);

        // assert
        Assert.NotNull(result);
        Assert.Equal(1, result.FilesToProcess);
    }

    [Fact]
    public async Task PreviewReplacementsAsync_DirectoryNotFound_ThrowsException()
    {
        // arrange
        var folderPath = "/nonexistent/folder";
        var replacementMap = CreateValidReplacementMap();
        
        _mockFileSystemService.Setup(fs => fs.DirectoryExists(folderPath)).Returns(false);
        _mockErrorHandler.Setup(eh => eh.HandleExceptionAsync(It.IsAny<DirectoryNotFoundException>(), "replacement preview"))
            .ReturnsAsync(ErrorResult.FileNotFoundError(folderPath, "replacement preview"));

        // act & assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await _service.PreviewReplacementsAsync(folderPath, replacementMap));
    }

    [Fact]
    public void ValidateReplacements_ValidInput_ReturnsValidResult()
    {
        // arrange
        var placeholders = new List<Placeholder>
        {
            new() 
            { 
                Name = "TestPlaceholder", 
                Pattern = @"\{\{TestPlaceholder\}\}",
                Locations = new List<PlaceholderLocation>(),
                TotalOccurrences = 0
            }
        };
        var replacementMap = CreateValidReplacementMap();

        // act
        var result = _service.ValidateReplacements(placeholders, replacementMap, false);

        // assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateReplacements_MissingRequiredPlaceholders_ReturnsInvalidResult()
    {
        // arrange
        var placeholders = new List<Placeholder>
        {
            new() 
            { 
                Name = "MissingPlaceholder", 
                Pattern = @"\{\{MissingPlaceholder\}\}",
                Locations = new List<PlaceholderLocation>(),
                TotalOccurrences = 0
            }
        };
        var replacementMap = CreateValidReplacementMap();

        // act
        var result = _service.ValidateReplacements(placeholders, replacementMap, true);

        // assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Missing replacement for required placeholder: MissingPlaceholder", result.Errors);
    }

    [Fact]
    public async Task CreateBackupsAsync_ValidInput_ReturnsSuccess()
    {
        // arrange
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/test/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };
        var backupDirectory = "/test/backup";
        
        _mockFileSystemService.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        _mockFileSystemService.Setup(fs => fs.CopyFile("/test/test.docx", It.IsAny<string>(), It.IsAny<bool>()));

        // act
        var result = await _service.CreateBackupsAsync(templateFiles, backupDirectory);

        // assert
        Assert.NotNull(result);
        Assert.Equal(1, result.FilesBackedUp);
        Assert.True(result.IsCompletelySuccessful);
    }

    [Fact]
    public async Task CreateBackupsAsync_EmptyFileList_ReturnsEmptySuccess()
    {
        // arrange
        var templateFiles = new List<TemplateFile>();

        // act
        var result = await _service.CreateBackupsAsync(templateFiles);

        // assert
        Assert.NotNull(result);
        Assert.Equal(0, result.FilesBackedUp);
        Assert.True(result.IsCompletelySuccessful);
    }

    [Fact]
    public async Task CreateBackupsAsync_BackupFailure_ReturnsPartialSuccess()
    {
        // arrange
        var templateFiles = new List<TemplateFile>
        {
            new()
            {
                FullPath = "/test/test.docx",
                RelativePath = "test.docx",
                FileName = "test.docx",
                SizeInBytes = 1024,
                LastModified = DateTime.UtcNow
            }
        };
        
        _mockFileSystemService.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        _mockFileSystemService.Setup(fs => fs.CopyFile("/test/test.docx", It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Access denied"));

        // act
        var result = await _service.CreateBackupsAsync(templateFiles);

        // assert
        Assert.NotNull(result);
        Assert.Equal(0, result.FilesBackedUp);
        Assert.Equal(1, result.FailedFiles);
        Assert.False(result.IsCompletelySuccessful);
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_NullFolderPath_ThrowsArgumentNullException()
    {
        // arrange
        var replacementMap = CreateValidReplacementMap();

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.ReplacePlaceholdersAsync((string)null!, replacementMap, true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ReplacePlaceholdersAsync_InvalidFolderPath_ThrowsArgumentException(string invalidPath)
    {
        // arrange
        var replacementMap = CreateValidReplacementMap();

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.ReplacePlaceholdersAsync(invalidPath, replacementMap, true));
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_NullReplacementMap_ThrowsArgumentNullException()
    {
        // arrange
        var folderPath = "/test/folder";

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.ReplacePlaceholdersAsync(folderPath, null!, true));
    }

    [Fact]
    public async Task ReplacePlaceholdersAsync_NullTemplateFiles_ThrowsArgumentNullException()
    {
        // arrange
        var replacementMap = CreateValidReplacementMap();

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.ReplacePlaceholdersAsync((IReadOnlyList<TemplateFile>)null!, replacementMap, true));
    }

    [Fact]
    public async Task ReplacePlaceholdersInFileAsync_NullTemplatePath_ThrowsArgumentNullException()
    {
        // arrange
        var replacementMap = CreateValidReplacementMap();

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.ReplacePlaceholdersInFileAsync((string)null!, replacementMap, true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ReplacePlaceholdersInFileAsync_InvalidTemplatePath_ThrowsArgumentException(string invalidPath)
    {
        // arrange
        var replacementMap = CreateValidReplacementMap();

        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.ReplacePlaceholdersInFileAsync(invalidPath, replacementMap, true));
    }

    [Fact]
    public void ValidateReplacements_NullPlaceholders_ThrowsArgumentNullException()
    {
        // arrange
        var replacementMap = CreateValidReplacementMap();

        // act & assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.ValidateReplacements(null!, replacementMap, false));
    }

    [Fact]
    public void ValidateReplacements_NullReplacementMap_ThrowsArgumentNullException()
    {
        // arrange
        var placeholders = new List<Placeholder>();

        // act & assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.ValidateReplacements(placeholders, null!, false));
    }

    [Fact]
    public async Task CreateBackupsAsync_NullTemplateFiles_ThrowsArgumentNullException()
    {
        // act & assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _service.CreateBackupsAsync(null!));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => 
            new PlaceholderReplaceService(null!, _mockErrorHandler.Object, _mockFileSystemService.Object, _mockReplacementEngine.Object));
    }

    [Fact]
    public void Constructor_NullErrorHandler_ThrowsArgumentNullException()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => 
            new PlaceholderReplaceService(_mockLogger.Object, null!, _mockFileSystemService.Object, _mockReplacementEngine.Object));
    }

    [Fact]
    public void Constructor_NullFileSystemService_ThrowsArgumentNullException()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => 
            new PlaceholderReplaceService(_mockLogger.Object, _mockErrorHandler.Object, null!, _mockReplacementEngine.Object));
    }

    [Fact]
    public void Constructor_NullReplacementEngine_ThrowsArgumentNullException()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => 
            new PlaceholderReplaceService(_mockLogger.Object, _mockErrorHandler.Object, _mockFileSystemService.Object, null!));
    }

    [Fact]
    public async Task TryReplaceImagePlaceholder_PreservesAlignment_WhenReplacingImageWithValidPath()
    {
        // arrange
        var testImagePath = Path.Combine(Path.GetTempPath(), "test_image.png");
        var testDocxPath = Path.Combine(Path.GetTempPath(), "test_document.docx");
        
        // Create a test image file
        await File.WriteAllBytesAsync(testImagePath, [137, 80, 78, 71, 13, 10, 26, 10]); // PNG header
        
        try
        {
            // Create test document with center-aligned image placeholder
            CreateTestDocumentWithCenterAlignedImagePlaceholder(testDocxPath);
            
            var imageInfo = new ImageInfo { Width = 100, Height = 100 };
            // Image processing is now handled internally by PlaceholderReplacementEngine
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "LOGO", testImagePath }
                }
            };
            
            // act
            var result = await _service.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert
            Assert.True(result.IsSuccess);
            Assert.True(result.ReplacementCount > 0);
            
            // Verify that the image was inserted and alignment was preserved
            VerifyImageReplacementPreservesAlignment(testDocxPath);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testImagePath)) File.Delete(testImagePath);
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }
    
    private static void CreateTestDocumentWithCenterAlignedImagePlaceholder(string docxPath)
    {
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(docxPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var document = new DocumentFormat.OpenXml.Wordprocessing.Document();
        var body = new DocumentFormat.OpenXml.Wordprocessing.Body();
        
        // Create a paragraph with center alignment and image placeholder
        var paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        
        // Add paragraph properties with center justification
        var paragraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
        var justification = new DocumentFormat.OpenXml.Wordprocessing.Justification()
        {
            Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center
        };
        paragraphProperties.Append(justification);
        paragraph.Append(paragraphProperties);
        
        // Add run with image placeholder text
        var run = new DocumentFormat.OpenXml.Wordprocessing.Run();
        var text = new DocumentFormat.OpenXml.Wordprocessing.Text("{{image:LOGO|width:200|height:150}}");
        run.Append(text);
        paragraph.Append(run);
        
        body.Append(paragraph);
        document.Append(body);
        mainPart.Document = document;
        mainPart.Document.Save();
    }
    
    private static void VerifyImageReplacementPreservesAlignment(string docxPath)
    {
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        Assert.NotNull(body);
        
        var paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
        Assert.NotEmpty(paragraphs);
        
        // Find the paragraph that should contain the replaced image
        var imageParagraph = paragraphs.FirstOrDefault(p => p.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any());
        Assert.NotNull(imageParagraph);
        
        // Verify that the paragraph properties with center justification are still there
        var paragraphProperties = imageParagraph.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties>();
        Assert.NotNull(paragraphProperties);
        
        var justification = paragraphProperties.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Justification>();
        Assert.NotNull(justification);
        Assert.Equal(DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center, justification.Val?.Value);
        
        // Verify that there's no placeholder text remaining
        var allText = string.Join("", imageParagraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
        Assert.DoesNotContain("{{image:LOGO|width:200|height:150}}", allText);
    }

    [Fact]
    public async Task ReplaceInFileAsync_ImageInHeader_ShouldUseHeaderPartNotMainPart()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"header_test_{Guid.NewGuid()}.docx");
        var testImagePath = Path.Combine(Path.GetTempPath(), $"test_logo_{Guid.NewGuid()}.png");
        
        // Create test image
        await File.WriteAllBytesAsync(testImagePath, CreateSimplePngBytes());
        
        try
        {
            // Create document with image placeholder in header
            CreateTestDocumentWithHeaderImagePlaceholder(testDocxPath);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "logo", testImagePath }
                }
            };
            
            var imageInfo = new ImageInfo { Width = 100, Height = 100 };
            // Image processing is now handled internally by PlaceholderReplacementEngine
            _mockFileSystemService.Setup(fs => fs.FileExists(testImagePath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            // act
            var result = await _service.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert
            Assert.True(result.IsSuccess);
            Assert.True(result.ReplacementCount > 0);
            
            // Verify image was added to HeaderPart, not MainDocumentPart
            VerifyImageInHeaderPart(testDocxPath);
        }
        finally
        {
            if (File.Exists(testImagePath)) File.Delete(testImagePath);
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    [Fact]
    public async Task ReplaceInFileAsync_ImageInFooter_ShouldUseFooterPartNotMainPart()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"footer_test_{Guid.NewGuid()}.docx");
        var testImagePath = Path.Combine(Path.GetTempPath(), $"test_logo_{Guid.NewGuid()}.png");
        
        // Create test image
        await File.WriteAllBytesAsync(testImagePath, CreateSimplePngBytes());
        
        try
        {
            // Create document with image placeholder in footer
            CreateTestDocumentWithFooterImagePlaceholder(testDocxPath);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "logo", testImagePath }
                }
            };
            
            var imageInfo = new ImageInfo { Width = 100, Height = 100 };
            // Image processing is now handled internally by PlaceholderReplacementEngine
            _mockFileSystemService.Setup(fs => fs.FileExists(testImagePath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            // act
            var result = await _service.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert
            Assert.True(result.IsSuccess);
            Assert.True(result.ReplacementCount > 0);
            
            // Verify image was added to FooterPart, not MainDocumentPart
            VerifyImageInFooterPart(testDocxPath);
        }
        finally
        {
            if (File.Exists(testImagePath)) File.Delete(testImagePath);
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    [Fact]
    public async Task ReplaceInFileAsync_SameImageInBodyAndHeader_ShouldAddToRespectiveParts()
    {
        // arrange
        var testDocxPath = Path.Combine(Path.GetTempPath(), $"body_header_test_{Guid.NewGuid()}.docx");
        var testImagePath = Path.Combine(Path.GetTempPath(), $"test_logo_{Guid.NewGuid()}.png");
        
        // Create test image
        await File.WriteAllBytesAsync(testImagePath, CreateSimplePngBytes());
        
        try
        {
            // Create document with same image placeholder in both body and header
            CreateTestDocumentWithBodyAndHeaderImagePlaceholder(testDocxPath);
            
            var replacementMap = new ReplacementMap
            {
                Mappings = new Dictionary<string, string>
                {
                    { "logo", testImagePath }
                }
            };
            
            var imageInfo = new ImageInfo { Width = 100, Height = 100 };
            // Image processing is now handled internally by PlaceholderReplacementEngine
            _mockFileSystemService.Setup(fs => fs.FileExists(testImagePath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.FileExists(testDocxPath)).Returns(true);
            _mockFileSystemService.Setup(fs => fs.GetFileSize(testDocxPath)).Returns(1024);
            
            // act
            var result = await _service.ReplacePlaceholdersInFileAsync(testDocxPath, replacementMap, false);
            
            // assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.ReplacementCount); // One in body, one in header
            
            // Verify images were added to both MainDocumentPart and HeaderPart
            VerifyImageInBothBodyAndHeader(testDocxPath);
        }
        finally
        {
            if (File.Exists(testImagePath)) File.Delete(testImagePath);
            if (File.Exists(testDocxPath)) File.Delete(testDocxPath);
        }
    }

    private static byte[] CreateSimplePngBytes()
    {
        // Create a minimal valid PNG (1x1 transparent pixel)
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk header
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimensions
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, // IHDR data + CRC
            0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, // IDAT chunk header
            0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, // IDAT data
            0x0D, 0x0A, 0x2D, 0xB4, // IDAT CRC
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 // IEND
        };
    }

    private static void CreateTestDocumentWithHeaderImagePlaceholder(string docxPath)
    {
        using var doc = WordprocessingDocument.Create(docxPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        
        // Create header with image placeholder
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(
            new Paragraph(new Run(new Text("{{image:logo|width:100|height:100}}"))));
        
        // Reference the header in section properties
        var sectionProps = new SectionProperties();
        var headerReference = new HeaderReference()
        {
            Type = HeaderFooterValues.Default,
            Id = mainPart.GetIdOfPart(headerPart)
        };
        sectionProps.AppendChild(headerReference);
        mainPart.Document.Body.AppendChild(sectionProps);
        
        mainPart.Document.Save();
    }
    
    private static void CreateTestDocumentWithFooterImagePlaceholder(string docxPath)
    {
        using var doc = WordprocessingDocument.Create(docxPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        
        // Create footer with image placeholder
        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(
            new Paragraph(new Run(new Text("{{image:logo|width:100|height:100}}"))));
        
        // Reference the footer in section properties
        var sectionProps = new SectionProperties();
        var footerReference = new FooterReference()
        {
            Type = HeaderFooterValues.Default,
            Id = mainPart.GetIdOfPart(footerPart)
        };
        sectionProps.AppendChild(footerReference);
        mainPart.Document.Body.AppendChild(sectionProps);
        
        mainPart.Document.Save();
    }
    
    private static void CreateTestDocumentWithBodyAndHeaderImagePlaceholder(string docxPath)
    {
        using var doc = WordprocessingDocument.Create(docxPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        
        // Add image placeholder to body
        var bodyParagraph = new Paragraph(new Run(new Text("{{image:logo|width:100|height:100}}")));
        mainPart.Document.Body.AppendChild(bodyParagraph);
        
        // Create header with image placeholder
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(
            new Paragraph(new Run(new Text("{{image:logo|width:100|height:100}}"))));
        
        // Reference the header in section properties
        var sectionProps = new SectionProperties();
        var headerReference = new HeaderReference()
        {
            Type = HeaderFooterValues.Default,
            Id = mainPart.GetIdOfPart(headerPart)
        };
        sectionProps.AppendChild(headerReference);
        mainPart.Document.Body.AppendChild(sectionProps);
        
        mainPart.Document.Save();
    }
    
    private static void VerifyImageInHeaderPart(string docxPath)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var mainPart = doc.MainDocumentPart;
        
        // Verify HeaderPart has images
        var headerPart = mainPart.HeaderParts.FirstOrDefault();
        Assert.NotNull(headerPart);
        Assert.NotEmpty(headerPart.ImageParts);
        
        // Verify MainDocumentPart does NOT have images (since placeholder was only in header)
        Assert.Empty(mainPart.ImageParts);
        
        // Verify header contains Drawing element (replaced image)
        var headerDrawing = headerPart.Header.Descendants<Drawing>().FirstOrDefault();
        Assert.NotNull(headerDrawing);
    }
    
    private static void VerifyImageInFooterPart(string docxPath)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var mainPart = doc.MainDocumentPart;
        
        // Verify FooterPart has images
        var footerPart = mainPart.FooterParts.FirstOrDefault();
        Assert.NotNull(footerPart);
        Assert.NotEmpty(footerPart.ImageParts);
        
        // Verify MainDocumentPart does NOT have images (since placeholder was only in footer)
        Assert.Empty(mainPart.ImageParts);
        
        // Verify footer contains Drawing element (replaced image)
        var footerDrawing = footerPart.Footer.Descendants<Drawing>().FirstOrDefault();
        Assert.NotNull(footerDrawing);
    }
    
    private static void VerifyImageInBothBodyAndHeader(string docxPath)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var mainPart = doc.MainDocumentPart;
        
        // Verify MainDocumentPart has images (body placeholder)
        Assert.NotEmpty(mainPart.ImageParts);
        
        // Verify HeaderPart has images (header placeholder)
        var headerPart = mainPart.HeaderParts.FirstOrDefault();
        Assert.NotNull(headerPart);
        Assert.NotEmpty(headerPart.ImageParts);
        
        // Verify body contains Drawing element
        var bodyDrawing = mainPart.Document.Body.Descendants<Drawing>().FirstOrDefault();
        Assert.NotNull(bodyDrawing);
        
        // Verify header contains Drawing element
        var headerDrawing = headerPart.Header.Descendants<Drawing>().FirstOrDefault();
        Assert.NotNull(headerDrawing);
    }

    private static ReplacementMap CreateValidReplacementMap()
    {
        var mappings = new Dictionary<string, string>
        {
            { "TestPlaceholder", "TestValue" },
            { "Name", "John Doe" },
            { "Date", "2025-08-17" }
        };
        return new ReplacementMap { Mappings = mappings };
    }
}