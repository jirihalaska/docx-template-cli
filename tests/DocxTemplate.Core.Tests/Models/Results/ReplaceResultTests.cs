using DocxTemplate.Core.Models.Results;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Tests.Models.Results;

public class ReplaceResultTests
{
    [Fact]
    public void ReplaceResult_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };
        var duration = TimeSpan.FromSeconds(2);

        // act
        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = duration,
            HasErrors = false
        };

        // assert
        result.FilesProcessed.Should().Be(2);
        result.TotalReplacements.Should().Be(8);
        result.FileResults.Should().BeEquivalentTo(fileResults);
        result.Duration.Should().Be(duration);
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ReplaceResult_SuccessfulFiles_ShouldReturnCountOfSuccessfulFiles()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Failure("/path/file2.docx", "Error occurred"),
            FileReplaceResult.Success("/path/file3.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 3,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = true
        };

        // act
        var successfulFiles = result.SuccessfulFiles;

        // assert
        successfulFiles.Should().Be(2);
    }

    [Fact]
    public void ReplaceResult_FailedFiles_ShouldReturnCountOfFailedFiles()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Failure("/path/file2.docx", "Error occurred"),
            FileReplaceResult.Success("/path/file3.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 3,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = true
        };

        // act
        var failedFiles = result.FailedFiles;

        // assert
        failedFiles.Should().Be(1);
    }

    [Fact]
    public void ReplaceResult_AverageReplacementsPerFile_ShouldCalculateCorrectly()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 6),
            FileReplaceResult.Success("/path/file2.docx", 4)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 10,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false
        };

        // act
        var average = result.AverageReplacementsPerFile;

        // assert
        average.Should().Be(5.0);
    }

    [Fact]
    public void ReplaceResult_AverageReplacementsPerFile_ShouldReturnZeroWhenNoSuccessfulFiles()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Failure("/path/file1.docx", "Error 1"),
            FileReplaceResult.Failure("/path/file2.docx", "Error 2")
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 0,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = true
        };

        // act
        var average = result.AverageReplacementsPerFile;

        // assert
        average.Should().Be(0.0);
    }

    [Fact]
    public void ReplaceResult_FilesPerSecond_ShouldCalculateCorrectly()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 4,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false
        };

        // act
        var filesPerSecond = result.FilesPerSecond;

        // assert
        filesPerSecond.Should().Be(2.0);
    }

    [Fact]
    public void ReplaceResult_FilesPerSecond_ShouldReturnZeroWhenDurationIsZero()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 1,
            TotalReplacements = 5,
            FileResults = fileResults,
            Duration = TimeSpan.Zero,
            HasErrors = false
        };

        // act
        var filesPerSecond = result.FilesPerSecond;

        // assert
        filesPerSecond.Should().Be(0.0);
    }

    [Fact]
    public void ReplaceResult_IsCompletelySuccessful_ShouldReturnTrueWhenNoErrors()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false
        };

        // act
        var isCompletelySuccessful = result.IsCompletelySuccessful;

        // assert
        isCompletelySuccessful.Should().BeTrue();
    }

    [Fact]
    public void ReplaceResult_IsCompletelySuccessful_ShouldReturnFalseWhenHasErrors()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Failure("/path/file2.docx", "Error")
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 5,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = true
        };

        // act
        var isCompletelySuccessful = result.IsCompletelySuccessful;

        // assert
        isCompletelySuccessful.Should().BeFalse();
    }

    [Fact]
    public void ReplaceResult_AllErrors_ShouldReturnAllErrorMessages()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Failure("/path/file2.docx", "Error 1"),
            FileReplaceResult.Failure("/path/file3.docx", "Error 2")
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 3,
            TotalReplacements = 5,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = true
        };

        // act
        var allErrors = result.AllErrors.ToList();

        // assert
        allErrors.Should().HaveCount(2);
        allErrors.Should().Contain("Error 1");
        allErrors.Should().Contain("Error 2");
    }

    [Fact]
    public void ReplaceResult_IsValid_ShouldReturnTrueForValidData()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false
        };

        // act
        var isValid = result.IsValid();

        // assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ReplaceResult_IsValid_ShouldReturnFalseWhenTotalReplacementsMismatch()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 10, // Wrong total
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false
        };

        // act
        var isValid = result.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ReplaceResult_IsValid_ShouldReturnFalseWhenFilesProcessedMismatch()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 3, // Wrong count
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false
        };

        // act
        var isValid = result.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ReplaceResult_IsValid_ShouldReturnFalseWhenHasErrorsMismatch()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Failure("/path/file2.docx", "Error")
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 2,
            TotalReplacements = 5,
            FileResults = fileResults,
            Duration = TimeSpan.FromSeconds(2),
            HasErrors = false // Should be true since there's a failure
        };

        // act
        var isValid = result.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ReplaceResult_GetSummary_ShouldReturnFormattedSummary()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3),
            FileReplaceResult.Failure("/path/file3.docx", "Error")
        };

        var result = new ReplaceResult
        {
            FilesProcessed = 3,
            TotalReplacements = 8,
            FileResults = fileResults,
            Duration = TimeSpan.FromMilliseconds(1500),
            HasErrors = true
        };

        // act
        var summary = result.GetSummary();

        // assert
        summary.Should().Contain("Processed 3 files");
        summary.Should().Contain("1500ms");
        summary.Should().Contain("Made 8 replacements");
        summary.Should().Contain("across 2 files");
        summary.Should().Contain("1 files failed");
        summary.Should().Contain("files/sec");
    }

    [Fact]
    public void ReplaceResult_Success_ShouldCreateValidResult()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Success("/path/file2.docx", 3)
        };
        var duration = TimeSpan.FromSeconds(2);

        // act
        var result = ReplaceResult.Success(fileResults, duration);

        // assert
        result.Should().NotBeNull();
        result.FilesProcessed.Should().Be(2);
        result.TotalReplacements.Should().Be(8);
        result.FileResults.Should().BeEquivalentTo(fileResults);
        result.Duration.Should().Be(duration);
        result.HasErrors.Should().BeFalse();
        result.IsValid().Should().BeTrue();
    }

    [Fact]
    public void ReplaceResult_Success_ShouldDetectErrorsInFileResults()
    {
        // arrange
        var fileResults = new List<FileReplaceResult>
        {
            FileReplaceResult.Success("/path/file1.docx", 5),
            FileReplaceResult.Failure("/path/file2.docx", "Error")
        };
        var duration = TimeSpan.FromSeconds(2);

        // act
        var result = ReplaceResult.Success(fileResults, duration);

        // assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeTrue();
        result.IsCompletelySuccessful.Should().BeFalse();
    }

    [Fact]
    public void ReplaceResult_Validation_ShouldFailWithNegativeValues()
    {
        // arrange
        var result = new ReplaceResult
        {
            FilesProcessed = -1,
            TotalReplacements = -5,
            FileResults = new List<FileReplaceResult>(),
            Duration = TimeSpan.FromSeconds(1),
            HasErrors = false
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(result, new ValidationContext(result), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("non-negative"));
    }
}