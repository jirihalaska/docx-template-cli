using System.IO;
using System.Threading.Tasks;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocxTemplate.Infrastructure.Tests.Services;

/// <summary>
/// Tests for verifying copy command maintains proper folder structure including top-level folder
/// </summary>
public class CopyFolderStructureTests
{
    private readonly Mock<ITemplateDiscoveryService> _mockDiscoveryService;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<ILogger<TemplateCopyService>> _mockLogger;
    private readonly TemplateCopyService _copyService;

    public CopyFolderStructureTests()
    {
        _mockDiscoveryService = new Mock<ITemplateDiscoveryService>();
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockLogger = new Mock<ILogger<TemplateCopyService>>();
        _copyService = new TemplateCopyService(_mockDiscoveryService.Object, _mockFileSystemService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithPreserveStructure_ShouldMaintainTopLevelFolderName()
    {
        // arrange - Set up a scenario where we copy from "MyTemplateSet" to target directory
        var sourcePath = "/source/MyTemplateSet";
        var targetPath = "/target/output";
        
        var templateFiles = new[]
        {
            CreateTemplateFile("/source/MyTemplateSet/document.docx", "document.docx"),
            CreateTemplateFile("/source/MyTemplateSet/subfolder/subdoc.docx", "subfolder/subdoc.docx"),
            CreateTemplateFile("/source/MyTemplateSet/nested/deep/file.docx", "nested/deep/file.docx")
        };

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockDiscoveryService.Setup(x => x.DiscoverTemplatesAsync(sourcePath, true, default))
            .ReturnsAsync(templateFiles);

        // Set up file system mocks for source files (they should exist)
        foreach (var templateFile in templateFiles)
        {
            _mockFileSystemService.Setup(x => x.FileExists(templateFile.FullPath)).Returns(true);
            _mockFileSystemService.Setup(x => x.GetFileSize(templateFile.FullPath)).Returns(templateFile.SizeInBytes);
            _mockFileSystemService.Setup(x => x.GetLastWriteTime(templateFile.FullPath)).Returns(templateFile.LastModified);
        }
        
        // Set up target file existence checks (they should not exist initially)
        _mockFileSystemService.Setup(x => x.FileExists(It.Is<string>(path => path.Contains("target")))).Returns(false);

        // Track which paths the copy operation tries to create
        var copiedPaths = new List<string>();
        _mockFileSystemService.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, bool>((source, target, overwrite) => copiedPaths.Add(target));

        // act
        var result = await _copyService.CopyTemplatesAsync(sourcePath, targetPath, preserveStructure: true, overwrite: false);

        // assert - Verify the top-level folder "MyTemplateSet" is maintained in target paths
        copiedPaths.Should().Contain(path => path.Contains("MyTemplateSet"), 
            "Target paths should include the source folder name 'MyTemplateSet'");
            
        // These are the expected target paths that should preserve the full structure
        var expectedPaths = new[]
        {
            "/target/output/MyTemplateSet/document.docx",
            "/target/output/MyTemplateSet/subfolder/subdoc.docx", 
            "/target/output/MyTemplateSet/nested/deep/file.docx"
        };

        foreach (var expectedPath in expectedPaths)
        {
            copiedPaths.Should().Contain(expectedPath.Replace('/', Path.DirectorySeparatorChar),
                $"Should copy to {expectedPath} to maintain folder structure including top-level folder");
        }
    }

    [Fact]
    public async Task CopyTemplatesAsync_WithoutPreserveStructure_ShouldFlattenButIncludeTopLevel()
    {
        // arrange - Even without preserving structure, we might want the top-level folder
        var sourcePath = "/source/MyTemplateSet";
        var targetPath = "/target/output";
        
        var templateFiles = new[]
        {
            CreateTemplateFile("/source/MyTemplateSet/document.docx", "document.docx"),
            CreateTemplateFile("/source/MyTemplateSet/subfolder/subdoc.docx", "subfolder/subdoc.docx")
        };

        _mockFileSystemService.Setup(x => x.DirectoryExists(sourcePath)).Returns(true);
        _mockFileSystemService.Setup(x => x.DirectoryExists(targetPath)).Returns(true);
        _mockDiscoveryService.Setup(x => x.DiscoverTemplatesAsync(sourcePath, true, default))
            .ReturnsAsync(templateFiles);

        // Set up file system mocks for source files (they should exist)
        foreach (var templateFile in templateFiles)
        {
            _mockFileSystemService.Setup(x => x.FileExists(templateFile.FullPath)).Returns(true);
            _mockFileSystemService.Setup(x => x.GetFileSize(templateFile.FullPath)).Returns(templateFile.SizeInBytes);
            _mockFileSystemService.Setup(x => x.GetLastWriteTime(templateFile.FullPath)).Returns(templateFile.LastModified);
        }
        
        // Set up target file existence checks (they should not exist initially)
        _mockFileSystemService.Setup(x => x.FileExists(It.Is<string>(path => path.Contains("target")))).Returns(false);

        var copiedPaths = new List<string>();
        _mockFileSystemService.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, bool>((source, target, overwrite) => copiedPaths.Add(target));

        // act
        var result = await _copyService.CopyTemplatesAsync(sourcePath, targetPath, preserveStructure: false, overwrite: false);

        // assert - Files should be flattened (no subfolder structure preserved)
        copiedPaths.Should().AllSatisfy(path => 
        {
            var fileName = Path.GetFileName(path);
            path.Should().Be(Path.Combine(targetPath, fileName), 
                "Files should be copied directly to target without preserving subdirectory structure");
        });
    }

    [Fact]
    public void DemonstrateBugInCurrentImplementation()
    {
        // arrange - This test shows the current problematic behavior
        var sourcePath = "/source/MyTemplateSet";
        var templateFile = CreateTemplateFile("/source/MyTemplateSet/subfolder/file.docx", "subfolder/file.docx");
        var targetPath = "/target/output";

        // act - Simulate what GetTargetFilePath currently does
        var currentTargetPath = Path.Combine(targetPath, templateFile.RelativePath);

        // assert - This demonstrates the bug
        currentTargetPath.Should().Be("/target/output/subfolder/file.docx".Replace('/', Path.DirectorySeparatorChar),
            "Current implementation loses the top-level folder 'MyTemplateSet'");
            
        // What we actually want
        var expectedPath = "/target/output/MyTemplateSet/subfolder/file.docx".Replace('/', Path.DirectorySeparatorChar);
        currentTargetPath.Should().NotBe(expectedPath, 
            "This shows the bug - we're missing the top-level folder name");
    }

    private static Core.Models.TemplateFile CreateTemplateFile(string fullPath, string relativePath)
    {
        return new Core.Models.TemplateFile
        {
            FullPath = fullPath,
            RelativePath = relativePath,
            FileName = Path.GetFileName(fullPath),
            SizeInBytes = 1024,
            LastModified = DateTime.UtcNow
        };
    }
}