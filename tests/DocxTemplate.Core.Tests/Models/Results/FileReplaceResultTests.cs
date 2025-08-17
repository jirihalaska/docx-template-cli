using DocxTemplate.Core.Models.Results;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Tests.Models.Results;

public class FileReplaceResultTests
{
    [Fact]
    public void FileReplaceResult_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var replacementCount = 5;
        var errorMessage = "Test error";
        var backupPath = "/backup/file.docx";
        var processingDuration = TimeSpan.FromMilliseconds(500);
        var finalSize = 2048L;

        // act
        var result = new FileReplaceResult
        {
            FilePath = filePath,
            ReplacementCount = replacementCount,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            BackupPath = backupPath,
            ProcessingDuration = processingDuration,
            FinalSizeBytes = finalSize
        };

        // assert
        result.FilePath.Should().Be(filePath);
        result.ReplacementCount.Should().Be(replacementCount);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.BackupPath.Should().Be(backupPath);
        result.ProcessingDuration.Should().Be(processingDuration);
        result.FinalSizeBytes.Should().Be(finalSize);
    }

    [Fact]
    public void FileReplaceResult_FileName_ShouldReturnFileNameFromPath()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/document.docx",
            ReplacementCount = 0,
            IsSuccess = true
        };

        // act
        var fileName = result.FileName;

        // assert
        fileName.Should().Be("document.docx");
    }

    [Fact]
    public void FileReplaceResult_HasBackup_ShouldReturnTrueWhenBackupPathExists()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/file.docx",
            ReplacementCount = 0,
            IsSuccess = true,
            BackupPath = "/backup/file.docx"
        };

        // act
        var hasBackup = result.HasBackup;

        // assert
        hasBackup.Should().BeTrue();
    }

    [Fact]
    public void FileReplaceResult_HasBackup_ShouldReturnFalseWhenBackupPathIsNull()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/file.docx",
            ReplacementCount = 0,
            IsSuccess = true,
            BackupPath = null
        };

        // act
        var hasBackup = result.HasBackup;

        // assert
        hasBackup.Should().BeFalse();
    }

    [Fact]
    public void FileReplaceResult_HasBackup_ShouldReturnFalseWhenBackupPathIsEmpty()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/file.docx",
            ReplacementCount = 0,
            IsSuccess = true,
            BackupPath = ""
        };

        // act
        var hasBackup = result.HasBackup;

        // assert
        hasBackup.Should().BeFalse();
    }

    [Fact]
    public void FileReplaceResult_DisplayResult_ShouldFormatSuccessCorrectly()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/document.docx",
            ReplacementCount = 5,
            IsSuccess = true,
            BackupPath = "/backup/document.docx"
        };

        // act
        var display = result.DisplayResult;

        // assert
        display.Should().Be("document.docx: 5 replacements (backup created)");
    }

    [Fact]
    public void FileReplaceResult_DisplayResult_ShouldFormatSuccessWithoutBackup()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/document.docx",
            ReplacementCount = 3,
            IsSuccess = true
        };

        // act
        var display = result.DisplayResult;

        // assert
        display.Should().Be("document.docx: 3 replacements");
    }

    [Fact]
    public void FileReplaceResult_DisplayResult_ShouldHandleSingularReplacement()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/document.docx",
            ReplacementCount = 1,
            IsSuccess = true
        };

        // act
        var display = result.DisplayResult;

        // assert
        display.Should().Be("document.docx: 1 replacement");
    }

    [Fact]
    public void FileReplaceResult_DisplayResult_ShouldFormatFailureCorrectly()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/document.docx",
            ReplacementCount = 0,
            IsSuccess = false,
            ErrorMessage = "File access denied"
        };

        // act
        var display = result.DisplayResult;

        // assert
        display.Should().Be("document.docx: Failed - File access denied");
    }

    [Fact]
    public void FileReplaceResult_DisplayResult_ShouldHandleNullErrorMessage()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/document.docx",
            ReplacementCount = 0,
            IsSuccess = false,
            ErrorMessage = null
        };

        // act
        var display = result.DisplayResult;

        // assert
        display.Should().Be("document.docx: Failed - Unknown error");
    }

    [Fact]
    public void FileReplaceResult_Success_ShouldCreateSuccessfulResult()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var replacementCount = 7;
        var backupPath = "/backup/file.docx";
        var duration = TimeSpan.FromMilliseconds(300);
        var finalSize = 1024L;

        // act
        var result = FileReplaceResult.Success(filePath, replacementCount, backupPath, duration, finalSize);

        // assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be(filePath);
        result.ReplacementCount.Should().Be(replacementCount);
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.BackupPath.Should().Be(backupPath);
        result.ProcessingDuration.Should().Be(duration);
        result.FinalSizeBytes.Should().Be(finalSize);
    }

    [Fact]
    public void FileReplaceResult_Success_ShouldCreateMinimalSuccessfulResult()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var replacementCount = 3;

        // act
        var result = FileReplaceResult.Success(filePath, replacementCount);

        // assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be(filePath);
        result.ReplacementCount.Should().Be(replacementCount);
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.BackupPath.Should().BeNull();
        result.ProcessingDuration.Should().BeNull();
        result.FinalSizeBytes.Should().BeNull();
    }

    [Fact]
    public void FileReplaceResult_Failure_ShouldCreateFailedResult()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var errorMessage = "Processing failed";
        var duration = TimeSpan.FromMilliseconds(100);

        // act
        var result = FileReplaceResult.Failure(filePath, errorMessage, duration);

        // assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be(filePath);
        result.ReplacementCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.BackupPath.Should().BeNull();
        result.ProcessingDuration.Should().Be(duration);
        result.FinalSizeBytes.Should().BeNull();
    }

    [Fact]
    public void FileReplaceResult_Failure_ShouldCreateMinimalFailedResult()
    {
        // arrange
        var filePath = "/path/to/file.docx";
        var errorMessage = "File not found";

        // act
        var result = FileReplaceResult.Failure(filePath, errorMessage);

        // assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be(filePath);
        result.ReplacementCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.BackupPath.Should().BeNull();
        result.ProcessingDuration.Should().BeNull();
        result.FinalSizeBytes.Should().BeNull();
    }

    [Fact]
    public void FileReplaceResult_Validation_ShouldFailWithEmptyFilePath()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "",
            ReplacementCount = 0,
            IsSuccess = true
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(result, new ValidationContext(result), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("File path is required"));
    }

    [Fact]
    public void FileReplaceResult_Validation_ShouldFailWithNegativeReplacementCount()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/file.docx",
            ReplacementCount = -1,
            IsSuccess = true
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(result, new ValidationContext(result), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("non-negative"));
    }

    [Fact]
    public void FileReplaceResult_Validation_ShouldPassWithValidData()
    {
        // arrange
        var result = new FileReplaceResult
        {
            FilePath = "/path/to/file.docx",
            ReplacementCount = 5,
            IsSuccess = true
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(result, new ValidationContext(result), validationResults, true);

        // assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}