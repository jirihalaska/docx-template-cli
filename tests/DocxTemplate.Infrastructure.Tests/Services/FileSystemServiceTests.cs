using System.Text;
using FluentAssertions;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Infrastructure.Services;

namespace DocxTemplate.Infrastructure.Tests.Services;

public class FileSystemServiceTests : IDisposable
{
    private readonly IFileSystemService _fileSystemService;
    private readonly string _testDirectory;
    private readonly string _testFile;
    private readonly string _testContent = "Test content with Czech characters: áčďéěíňóřšťúůýž";

    public FileSystemServiceTests()
    {
        _fileSystemService = new FileSystemService();
        _testDirectory = Path.Combine(Path.GetTempPath(), "DocxTemplateTests", Guid.NewGuid().ToString());
        _testFile = Path.Combine(_testDirectory, "test.txt");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void FileExists_WhenFileExists_ReturnsTrue()
    {
        // arrange
        File.WriteAllText(_testFile, _testContent);

        // act
        var result = _fileSystemService.FileExists(_testFile);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        // arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // act
        var result = _fileSystemService.FileExists(nonExistentFile);

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DirectoryExists_WhenDirectoryExists_ReturnsTrue()
    {
        // arrange & act
        var result = _fileSystemService.DirectoryExists(_testDirectory);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DirectoryExists_WhenDirectoryDoesNotExist_ReturnsFalse()
    {
        // arrange
        var nonExistentDirectory = Path.Combine(_testDirectory, "nonexistent");

        // act
        var result = _fileSystemService.DirectoryExists(nonExistentDirectory);

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReadAllTextAsync_WhenFileExists_ReturnsContent()
    {
        // arrange
        await File.WriteAllTextAsync(_testFile, _testContent, Encoding.UTF8);

        // act
        var result = await _fileSystemService.ReadAllTextAsync(_testFile);

        // assert
        result.Should().Be(_testContent);
    }

    [Fact]
    public async Task ReadAllTextAsync_WhenFileDoesNotExist_ThrowsFileAccessException()
    {
        // arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // act & assert
        await _fileSystemService.Invoking(fs => fs.ReadAllTextAsync(nonExistentFile))
            .Should().ThrowAsync<FileAccessException>()
            .WithMessage($"File not found: {nonExistentFile}");
    }

    [Fact]
    public async Task WriteAllTextAsync_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // arrange
        var subDirectory = Path.Combine(_testDirectory, "subdir");
        var fileInSubDir = Path.Combine(subDirectory, "test.txt");

        // act
        await _fileSystemService.WriteAllTextAsync(fileInSubDir, _testContent);

        // assert
        Directory.Exists(subDirectory).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(fileInSubDir);
        writtenContent.Should().Be(_testContent);
    }

    [Fact]
    public async Task WriteAllTextAsync_WithCzechCharacters_PreservesEncoding()
    {
        // arrange & act
        await _fileSystemService.WriteAllTextAsync(_testFile, _testContent, Encoding.UTF8);

        // assert
        var readContent = await _fileSystemService.ReadAllTextAsync(_testFile, Encoding.UTF8);
        readContent.Should().Be(_testContent);
    }

    [Fact]
    public async Task ReadAllBytesAsync_WhenFileExists_ReturnsBytes()
    {
        // arrange
        var testBytes = Encoding.UTF8.GetBytes(_testContent);
        await File.WriteAllBytesAsync(_testFile, testBytes);

        // act
        var result = await _fileSystemService.ReadAllBytesAsync(_testFile);

        // assert
        result.Should().BeEquivalentTo(testBytes);
    }

    [Fact]
    public async Task WriteAllBytesAsync_WritesCorrectBytes()
    {
        // arrange
        var testBytes = Encoding.UTF8.GetBytes(_testContent);

        // act
        await _fileSystemService.WriteAllBytesAsync(_testFile, testBytes);

        // assert
        var writtenBytes = await File.ReadAllBytesAsync(_testFile);
        writtenBytes.Should().BeEquivalentTo(testBytes);
    }

    [Fact]
    public void EnumerateFiles_WhenDirectoryHasFiles_ReturnsFiles()
    {
        // arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.docx");
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        // act
        var result = _fileSystemService.EnumerateFiles(_testDirectory, "*.txt").ToList();

        // assert
        result.Should().Contain(file1);
        result.Should().NotContain(file2);
    }

    [Fact]
    public void EnumerateDirectories_WhenDirectoryHasSubdirectories_ReturnsDirectories()
    {
        // arrange
        var subDir1 = Path.Combine(_testDirectory, "subdir1");
        var subDir2 = Path.Combine(_testDirectory, "subdir2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        // act
        var result = _fileSystemService.EnumerateDirectories(_testDirectory).ToList();

        // assert
        result.Should().Contain(subDir1);
        result.Should().Contain(subDir2);
    }

    [Fact]
    public void CreateDirectory_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // arrange
        var newDirectory = Path.Combine(_testDirectory, "newdir");

        // act
        _fileSystemService.CreateDirectory(newDirectory);

        // assert
        Directory.Exists(newDirectory).Should().BeTrue();
    }

    [Fact]
    public void DeleteFile_WhenFileExists_DeletesFile()
    {
        // arrange
        File.WriteAllText(_testFile, _testContent);
        File.Exists(_testFile).Should().BeTrue();

        // act
        _fileSystemService.DeleteFile(_testFile);

        // assert
        File.Exists(_testFile).Should().BeFalse();
    }

    [Fact]
    public void CopyFile_WhenSourceExists_CopiesFile()
    {
        // arrange
        File.WriteAllText(_testFile, _testContent);
        var destinationFile = Path.Combine(_testDirectory, "copy.txt");

        // act
        _fileSystemService.CopyFile(_testFile, destinationFile);

        // assert
        File.Exists(destinationFile).Should().BeTrue();
        var copiedContent = File.ReadAllText(destinationFile);
        copiedContent.Should().Be(_testContent);
    }

    [Fact]
    public void GetFileName_ReturnsCorrectFileName()
    {
        // arrange & act
        var result = _fileSystemService.GetFileName(_testFile);

        // assert
        result.Should().Be("test.txt");
    }

    [Fact]
    public void GetFileNameWithoutExtension_ReturnsCorrectName()
    {
        // arrange & act
        var result = _fileSystemService.GetFileNameWithoutExtension(_testFile);

        // assert
        result.Should().Be("test");
    }

    [Fact]
    public void GetExtension_ReturnsCorrectExtension()
    {
        // arrange & act
        var result = _fileSystemService.GetExtension(_testFile);

        // assert
        result.Should().Be(".txt");
    }

    [Fact]
    public void Combine_CombinesPathsCorrectly()
    {
        // arrange & act
        var result = _fileSystemService.Combine("path1", "path2", "file.txt");

        // assert
        result.Should().Be(Path.Combine("path1", "path2", "file.txt"));
    }

    [Fact]
    public void GetFileSize_WhenFileExists_ReturnsSize()
    {
        // arrange
        File.WriteAllText(_testFile, _testContent);
        var expectedSize = new FileInfo(_testFile).Length;

        // act
        var result = _fileSystemService.GetFileSize(_testFile);

        // assert
        result.Should().Be(expectedSize);
    }

    [Fact]
    public async Task CreateBackupAsync_WhenFileExists_CreatesBackup()
    {
        // arrange
        await File.WriteAllTextAsync(_testFile, _testContent);

        // act
        var backupPath = await _fileSystemService.CreateBackupAsync(_testFile);

        // assert
        File.Exists(backupPath).Should().BeTrue();
        var backupContent = await File.ReadAllTextAsync(backupPath);
        backupContent.Should().Be(_testContent);
        backupPath.Should().Contain(".backup.");
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WhenBackupExists_RestoresFile()
    {
        // arrange
        await File.WriteAllTextAsync(_testFile, _testContent);
        var backupPath = await _fileSystemService.CreateBackupAsync(_testFile);
        await File.WriteAllTextAsync(_testFile, "modified content");

        // act
        await _fileSystemService.RestoreFromBackupAsync(backupPath, _testFile);

        // assert
        var restoredContent = await File.ReadAllTextAsync(_testFile);
        restoredContent.Should().Be(_testContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FileExists_WithInvalidPath_ThrowsArgumentException(string invalidPath)
    {
        // act & assert
        _fileSystemService.Invoking(fs => fs.FileExists(invalidPath))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DirectoryExists_WithInvalidPath_ThrowsArgumentException(string invalidPath)
    {
        // act & assert
        _fileSystemService.Invoking(fs => fs.DirectoryExists(invalidPath))
            .Should().Throw<ArgumentException>();
    }
}