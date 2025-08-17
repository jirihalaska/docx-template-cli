using DocxTemplate.Core.Models.Results;
using Xunit;

namespace DocxTemplate.Core.Tests.Models.Results;

public class ErrorResultTests
{
    [Fact]
    public void FileAccessError_CreatesCorrectErrorResult()
    {
        // arrange
        const string filePath = "test.docx";
        const string operation = "file processing";
        var exception = new UnauthorizedAccessException("Access denied");

        // act
        var result = ErrorResult.FileAccessError(filePath, operation, exception);

        // assert
        Assert.Equal("FileAccess", result.ErrorType);
        Assert.Contains(filePath, result.Message);
        Assert.Contains(operation, result.Message);
        Assert.Equal(operation, result.OperationContext);
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal(2, result.ExitCode);
        Assert.False(result.IsCritical);
        Assert.Contains(exception.ToString(), result.Details);
    }

    [Fact]
    public void FileNotFoundError_CreatesCorrectErrorResult()
    {
        // arrange
        const string filePath = "missing.docx";
        const string operation = "file reading";

        // act
        var result = ErrorResult.FileNotFoundError(filePath, operation);

        // assert
        Assert.Equal("FileNotFound", result.ErrorType);
        Assert.Contains(filePath, result.Message);
        Assert.Contains(operation, result.Message);
        Assert.Equal(operation, result.OperationContext);
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal(2, result.ExitCode);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public void ValidationError_CreatesCorrectErrorResult()
    {
        // arrange
        const string message = "Invalid input format";
        const string operation = "input validation";
        const string details = "Additional validation details";

        // act
        var result = ErrorResult.ValidationError(message, operation, details);

        // assert
        Assert.Equal("Validation", result.ErrorType);
        Assert.Equal(message, result.Message);
        Assert.Equal(operation, result.OperationContext);
        Assert.Equal(details, result.Details);
        Assert.Equal(1, result.ExitCode);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public void DocumentProcessingError_CreatesCorrectErrorResult()
    {
        // arrange
        const string filePath = "corrupt.docx";
        const string operation = "document processing";
        var exception = new InvalidOperationException("Document is corrupted");

        // act
        var result = ErrorResult.DocumentProcessingError(filePath, operation, exception);

        // assert
        Assert.Equal("DocumentProcessing", result.ErrorType);
        Assert.Contains(filePath, result.Message);
        Assert.Contains(operation, result.Message);
        Assert.Equal(operation, result.OperationContext);
        Assert.Equal(filePath, result.FilePath);
        Assert.Equal(3, result.ExitCode);
        Assert.False(result.IsCritical);
        Assert.Contains(exception.ToString(), result.Details);
    }

    [Fact]
    public void CriticalError_CreatesCorrectErrorResult()
    {
        // arrange
        const string message = "System failure occurred";
        const string operation = "system operation";
        var exception = new OutOfMemoryException("Out of memory");

        // act
        var result = ErrorResult.CriticalError(message, operation, exception);

        // assert
        Assert.Equal("Critical", result.ErrorType);
        Assert.Equal(message, result.Message);
        Assert.Equal(operation, result.OperationContext);
        Assert.Equal(99, result.ExitCode);
        Assert.True(result.IsCritical);
        Assert.Contains(exception.ToString(), result.Details);
    }

    [Fact]
    public void ErrorResult_SetsTimestampCorrectly()
    {
        // arrange
        var beforeCreation = DateTime.UtcNow;

        // act
        var result = ErrorResult.ValidationError("Test error", "test operation");
        var afterCreation = DateTime.UtcNow;

        // assert
        Assert.True(result.Timestamp >= beforeCreation);
        Assert.True(result.Timestamp <= afterCreation);
    }

    [Fact]
    public void ErrorResult_WithAllProperties_InitializesCorrectly()
    {
        // arrange & act
        var result = new ErrorResult
        {
            ErrorType = "TestError",
            Message = "Test message",
            OperationContext = "test operation",
            Details = "test details",
            FilePath = "test.txt",
            ExitCode = 5,
            IsCritical = true
        };

        // assert
        Assert.Equal("TestError", result.ErrorType);
        Assert.Equal("Test message", result.Message);
        Assert.Equal("test operation", result.OperationContext);
        Assert.Equal("test details", result.Details);
        Assert.Equal("test.txt", result.FilePath);
        Assert.Equal(5, result.ExitCode);
        Assert.True(result.IsCritical);
    }
}