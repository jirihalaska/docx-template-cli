using DocxTemplate.Core.Models.Results;
using Xunit;

namespace DocxTemplate.Core.Tests.Models.Results;

public class CommandResultTests
{
    [Fact]
    public void Success_WithData_CreatesSuccessfulResult()
    {
        // arrange
        const string testData = "test result";

        // act
        var result = CommandResult<string>.Success(testData);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(testData, result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WithoutData_CreatesSuccessfulResult()
    {
        // arrange & act
        var result = CommandResult.Success();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WithErrorResult_CreatesFailedResult()
    {
        // arrange
        var error = ErrorResult.ValidationError("Test error", "test operation");

        // act
        var result = CommandResult<string>.Failure(error);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_WithException_CreatesFailedResult()
    {
        // arrange
        var exception = new ArgumentException("Invalid argument");
        const string operationContext = "test operation";

        // act
        var result = CommandResult<string>.Failure(exception, operationContext);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.Error);
        Assert.Equal("Validation", result.Error.ErrorType);
        Assert.Contains("Invalid argument", result.Error.Message);
        Assert.Equal(operationContext, result.Error.OperationContext);
    }

    [Fact]
    public void Failure_WithFileNotFoundException_CreatesCorrectErrorType()
    {
        // arrange
        var exception = new FileNotFoundException("File not found", "test.txt");
        const string operationContext = "file reading";

        // act
        var result = CommandResult<string>.Failure(exception, operationContext);

        // assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("FileNotFound", result.Error.ErrorType);
        Assert.Equal(2, result.Error.ExitCode);
    }

    [Fact]
    public void Failure_WithUnauthorizedAccessException_CreatesCorrectErrorType()
    {
        // arrange
        var exception = new UnauthorizedAccessException("Access denied");
        const string operationContext = "file access";

        // act
        var result = CommandResult<string>.Failure(exception, operationContext);

        // assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("FileAccess", result.Error.ErrorType);
        Assert.Equal(3, result.Error.ExitCode);
    }

    [Fact]
    public void Failure_WithUnknownException_CreatesCriticalError()
    {
        // arrange
        var exception = new InvalidOperationException("Unknown error");
        const string operationContext = "unknown operation";

        // act
        var result = CommandResult<string>.Failure(exception, operationContext);

        // assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Critical", result.Error.ErrorType);
        Assert.True(result.Error.IsCritical);
        Assert.Equal(99, result.Error.ExitCode);
    }

    [Fact]
    public void CommandResult_SetsCompletedAtCorrectly()
    {
        // arrange
        var beforeCreation = DateTime.UtcNow;

        // act
        var result = CommandResult<string>.Success("test");
        var afterCreation = DateTime.UtcNow;

        // assert
        Assert.True(result.CompletedAt >= beforeCreation);
        Assert.True(result.CompletedAt <= afterCreation);
    }

    [Fact]
    public void CommandResult_WithoutDataType_FailureWithErrorResult_Works()
    {
        // arrange
        var error = ErrorResult.ValidationError("Test error", "test operation");

        // act
        var result = CommandResult.Failure(error);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void CommandResult_WithoutDataType_FailureWithException_Works()
    {
        // arrange
        var exception = new ArgumentException("Invalid argument");
        const string operationContext = "test operation";

        // act
        var result = CommandResult.Failure(exception, operationContext);

        // assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("Validation", result.Error.ErrorType);
    }
}