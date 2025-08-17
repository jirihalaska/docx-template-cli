using DocxTemplate.Core.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Core.Tests.Models;

public class TemplateTests
{
    [Fact]
    public void Template_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // arrange
        var fileName = "template.docx";
        var relativePath = "subfolder/template.docx";
        var fullPath = "/path/to/subfolder/template.docx";
        var sizeBytes = 1024L;
        var lastModified = DateTime.UtcNow;

        // act
        var template = new Template
        {
            FileName = fileName,
            RelativePath = relativePath,
            FullPath = fullPath,
            SizeBytes = sizeBytes,
            LastModified = lastModified
        };

        // assert
        template.FileName.Should().Be(fileName);
        template.RelativePath.Should().Be(relativePath);
        template.FullPath.Should().Be(fullPath);
        template.SizeBytes.Should().Be(sizeBytes);
        template.LastModified.Should().Be(lastModified);
    }

    [Fact]
    public void Template_DisplaySize_ShouldReturnHumanReadableSize()
    {
        // arrange
        var template = new Template
        {
            FileName = "test.docx",
            RelativePath = "test.docx",
            FullPath = "/test.docx",
            SizeBytes = 1536,
            LastModified = DateTime.UtcNow
        };

        // act
        var displaySize = template.DisplaySize;

        // assert
        displaySize.Should().Be("1.5 KB");
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void Template_DisplaySize_ShouldFormatDifferentSizesCorrectly(long bytes, string expected)
    {
        // arrange
        var template = new Template
        {
            FileName = "test.docx",
            RelativePath = "test.docx",
            FullPath = "/test.docx",
            SizeBytes = bytes,
            LastModified = DateTime.UtcNow
        };

        // act
        var displaySize = template.DisplaySize;

        // assert
        displaySize.Should().Be(expected);
    }

    [Fact]
    public void Template_IsValid_ShouldReturnFalseForNonExistentFile()
    {
        // arrange
        var template = new Template
        {
            FileName = "template.docx",
            RelativePath = "template.docx",
            FullPath = "/nonexistent/path/template.docx",
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // act
        var isValid = template.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("template.txt")]
    [InlineData("template.pdf")]
    [InlineData("template")]
    public void Template_IsValid_ShouldReturnFalseForNonDocxFiles(string fileName)
    {
        // arrange
        var template = new Template
        {
            FileName = fileName,
            RelativePath = fileName,
            FullPath = $"/path/{fileName}",
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // act
        var isValid = template.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../template.docx")]
    [InlineData("folder/../template.docx")]
    public void Template_IsValid_ShouldReturnFalseForInvalidRelativePaths(string relativePath)
    {
        // arrange
        var template = new Template
        {
            FileName = "template.docx",
            RelativePath = relativePath,
            FullPath = "/path/template.docx",
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // act
        var isValid = template.IsValid();

        // assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Template_Validation_ShouldFailWithEmptyFileName()
    {
        // arrange
        var template = new Template
        {
            FileName = string.Empty,
            RelativePath = "template.docx",
            FullPath = "/valid/path.docx",
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(template, new ValidationContext(template), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("file name is required"));
    }

    [Fact]
    public void Template_Validation_ShouldFailWithEmptyRelativePath()
    {
        // arrange
        var template = new Template
        {
            FileName = "template.docx",
            RelativePath = string.Empty,
            FullPath = "/path/template.docx",
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(template, new ValidationContext(template), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("relative path is required"));
    }

    [Fact]
    public void Template_Validation_ShouldFailWithEmptyFullPath()
    {
        // arrange
        var template = new Template
        {
            FileName = "template.docx",
            RelativePath = "template.docx",
            FullPath = string.Empty,
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(template, new ValidationContext(template), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("full path is required"));
    }

    [Fact]
    public void Template_Validation_ShouldFailWithNegativeFileSize()
    {
        // arrange
        var template = new Template
        {
            FileName = "template.docx",
            RelativePath = "template.docx",
            FullPath = "/valid/path.docx",
            SizeBytes = -1,
            LastModified = DateTime.UtcNow
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(template, new ValidationContext(template), validationResults, true);

        // assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle(r => r.ErrorMessage!.Contains("non-negative"));
    }

    [Fact]
    public void Template_Validation_ShouldPassWithValidData()
    {
        // arrange
        var template = new Template
        {
            FileName = "template.docx",
            RelativePath = "folder/template.docx",
            FullPath = "/valid/path/folder/template.docx",
            SizeBytes = 1024,
            LastModified = DateTime.UtcNow.AddMinutes(-1)
        };

        // act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(template, new ValidationContext(template), validationResults, true);

        // assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}