using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace DocxTemplate.Core.Tests.ErrorHandling;

public class ErrorHandlerTests
{
    private readonly Mock<ILogger<ErrorHandler>> _mockLogger;
    private readonly IErrorHandler _errorHandler;

    public ErrorHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ErrorHandler>>();
        _errorHandler = new ErrorHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithFileNotFoundException_ReturnsAppropriateErrorResult()
    {
        // arrange
        var exception = new FileNotFoundException("Test file not found", "test.docx");
        const string operationContext = "file processing";

        // act
        var result = await _errorHandler.HandleExceptionAsync(exception, operationContext);

        // assert
        Assert.Equal("FileNotFound", result.ErrorType);
        Assert.Contains("test.docx", result.Message);
        Assert.Equal(operationContext, result.OperationContext);
        Assert.Equal(2, result.ExitCode);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithUnauthorizedAccessException_ReturnsAppropriateErrorResult()
    {
        // arrange
        var exception = new UnauthorizedAccessException("Access denied to file");
        const string operationContext = "file access";

        // act
        var result = await _errorHandler.HandleExceptionAsync(exception, operationContext);

        // assert
        Assert.Equal("FileAccess", result.ErrorType);
        Assert.Contains("Access denied", result.Message);
        Assert.Equal(operationContext, result.OperationContext);
        Assert.Equal(3, result.ExitCode);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithDomainException_ReturnsAppropriateErrorResult()
    {
        // arrange
        var exception = new ReplacementValidationException("Invalid replacement mapping");
        const string operationContext = "replacement validation";

        // act
        var result = await _errorHandler.HandleExceptionAsync(exception, operationContext);

        // assert
        Assert.Equal("ReplacementValidationException", result.ErrorType);
        Assert.Contains("Invalid replacement mapping", result.Message);
        Assert.Equal(operationContext, result.OperationContext);
        Assert.Equal(1, result.ExitCode);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithCriticalException_ReturnsCriticalErrorResult()
    {
        // arrange
        var exception = new OutOfMemoryException("Not enough memory");
        const string operationContext = "memory allocation";

        // act
        var result = await _errorHandler.HandleExceptionAsync(exception, operationContext);

        // assert
        Assert.True(result.IsCritical);
        Assert.Equal(99, result.ExitCode);
        Assert.Contains("Not enough memory", result.Message);
    }

    [Theory]
    [InlineData(typeof(FileNotFoundException), 2)]
    [InlineData(typeof(UnauthorizedAccessException), 3)]
    [InlineData(typeof(ArgumentException), 1)]
    [InlineData(typeof(DocumentProcessingException), 4)]
    [InlineData(typeof(OutOfMemoryException), 99)]
    public void GetExitCode_WithVariousExceptions_ReturnsCorrectExitCode(Type exceptionType, int expectedExitCode)
    {
        // arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;

        // act
        var exitCode = _errorHandler.GetExitCode(exception);

        // assert
        Assert.Equal(expectedExitCode, exitCode);
    }

    [Theory]
    [InlineData(typeof(OutOfMemoryException), true)]
    [InlineData(typeof(StackOverflowException), true)]
    [InlineData(typeof(SystemException), true)]
    [InlineData(typeof(FileNotFoundException), false)]
    [InlineData(typeof(ArgumentException), false)]
    [InlineData(typeof(DocumentProcessingException), false)]
    public void IsCriticalError_WithVariousExceptions_ReturnsCorrectCriticality(Type exceptionType, bool expectedCritical)
    {
        // arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;

        // act
        var isCritical = _errorHandler.IsCriticalError(exception);

        // assert
        Assert.Equal(expectedCritical, isCritical);
    }

    [Fact]
    public void FormatErrorMessage_WithTemplateNotFoundException_ReturnsFormattedMessage()
    {
        // arrange
        var exception = new TemplateNotFoundException("Template file not found");
        const string context = "template discovery";

        // act
        var message = _errorHandler.FormatErrorMessage(exception, context);

        // assert
        Assert.StartsWith("Template not found:", message);
        Assert.Contains("Template file not found", message);
    }

    [Fact]
    public void FormatErrorMessage_WithFileNotFoundException_ReturnsFormattedMessage()
    {
        // arrange
        var exception = new FileNotFoundException("File not found", "test.docx");
        const string context = "file processing";

        // act
        var message = _errorHandler.FormatErrorMessage(exception, context);

        // assert
        Assert.StartsWith("File not found:", message);
        Assert.Contains("test.docx", message);
    }

    [Fact]
    public void FormatErrorMessage_WithJsonException_ReturnsFormattedMessage()
    {
        // arrange
        var exception = new JsonException("Invalid JSON format at line 5");
        const string context = "JSON parsing";

        // act
        var message = _errorHandler.FormatErrorMessage(exception, context);

        // assert
        Assert.StartsWith("Invalid JSON format:", message);
        Assert.Contains("Invalid JSON format at line 5", message);
    }

    [Fact]
    public void FormatErrorMessage_WithGenericException_ReturnsContextualMessage()
    {
        // arrange
        var exception = new InvalidOperationException("Invalid operation");
        const string context = "test operation";

        // act
        var message = _errorHandler.FormatErrorMessage(exception, context);

        // assert
        Assert.Contains("test operation", message);
        Assert.Contains("Invalid operation", message);
    }

    [Fact]
    public async Task HandleExceptionAsync_LogsErrorWithAppropriateLevel()
    {
        // arrange
        var exception = new FileNotFoundException("Test file not found");
        const string operationContext = "file processing";

        // act
        await _errorHandler.HandleExceptionAsync(exception, operationContext);

        // assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during file processing")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithCriticalError_LogsAsError()
    {
        // arrange
        var exception = new OutOfMemoryException("Out of memory");
        const string operationContext = "memory allocation";

        // act
        await _errorHandler.HandleExceptionAsync(exception, operationContext);

        // assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Critical error during memory allocation")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // arrange & act & assert
        Assert.Throws<ArgumentNullException>(() => new ErrorHandler(null!));
    }
}