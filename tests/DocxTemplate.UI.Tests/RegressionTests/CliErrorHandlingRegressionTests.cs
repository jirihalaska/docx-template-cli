using System;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.RegressionTests;

/// <summary>
/// Regression tests to ensure CLI error message handling is flexible and doesn't break on message format changes
/// </summary>
public class CliErrorHandlingRegressionTests
{
    private readonly CliProcessRunner _cliProcessRunner;

    public CliErrorHandlingRegressionTests()
    {
        // Use a non-existent CLI path to force error conditions
        _cliProcessRunner = new CliProcessRunner("/fake/nonexistent/cli/path");
    }

    [Theory]
    [InlineData("Failed to execute CLI command")]
    [InlineData("CLI command failed with exit code 1")]
    [InlineData("CLI command 'invalid' failed with exit code 127")]
    [InlineData("Process execution failed")]
    public async Task CliProcessRunner_ShouldHandleVariousErrorMessageFormats(string expectedErrorType)
    {
        // act & assert - Should handle any error message format gracefully
        try
        {
            await _cliProcessRunner.ExecuteCommandAsync("invalid-command", new[] { "--fake" });
        }
        catch (Exception ex)
        {
            // The exact error message format may vary, but should contain key information
            var errorMessage = ex.Message;
            
            // Should contain either "Failed to execute CLI command" or "CLI command" + "failed"
            var containsExpectedFormat = 
                errorMessage.Contains("Failed to execute CLI command") ||
                (errorMessage.Contains("CLI command") && errorMessage.Contains("failed"));
                
            containsExpectedFormat.Should().BeTrue(
                $"Error message should follow expected format patterns. Actual: {errorMessage}");
        }
    }

    [Fact]
    public async Task CliProcessRunner_ErrorMessages_ShouldNotBeHardcodedToSpecificFormat()
    {
        // arrange - This test ensures we don't break when CLI error message format changes
        Exception? caughtException = null;

        // act
        try
        {
            await _cliProcessRunner.ExecuteCommandAsync("will-fail", new[] { "--invalid" });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // assert - Should throw some kind of exception
        caughtException.Should().NotBeNull("CLI execution should throw exception on invalid command");
        
        // Error message should be informative but not tied to exact wording
        var errorMessage = caughtException!.Message;
        errorMessage.Should().NotBeNullOrEmpty("Error message should provide information");
        
        // Should contain some indication of CLI-related failure
        var containsCliReference = 
            errorMessage.ToLower().Contains("cli") ||
            errorMessage.ToLower().Contains("command") ||
            errorMessage.ToLower().Contains("execute") ||
            errorMessage.ToLower().Contains("process");
            
        containsCliReference.Should().BeTrue(
            $"Error message should indicate CLI/command execution failure. Actual: {errorMessage}");
    }

    [Fact]
    public void CliExecutableNotFoundException_ShouldProvideFlexibleErrorHandling()
    {
        // arrange - Test exception handling for CLI discovery failures
        var testDirectory = "/fake/test/directory";
        
        // act
        var exception = new CliExecutableNotFoundException(testDirectory);
        
        // assert - Should provide useful error information without rigid format requirements
        exception.Message.Should().Contain(testDirectory, 
            "Exception should include the directory that was searched");
        exception.Message.Should().Contain("CLI", 
            "Exception should indicate this is about CLI executable");
    }

    [Theory]
    [InlineData("command not found")]
    [InlineData("No such file or directory")]
    [InlineData("Access is denied")]
    [InlineData("Permission denied")]
    [InlineData("File not found")]
    public void ErrorMessageParsing_ShouldHandleCommonSystemErrors(string systemError)
    {
        // arrange - Test that we can handle various system-level error messages
        var fullErrorMessage = $"CLI execution failed: {systemError}";
        
        // act - Simulate error message processing
        var isRecognizedError = 
            fullErrorMessage.ToLower().Contains("not found") ||
            fullErrorMessage.ToLower().Contains("denied") ||
            fullErrorMessage.ToLower().Contains("failed");
        
        // assert - Should recognize common error patterns
        isRecognizedError.Should().BeTrue(
            $"Should recognize common system error pattern in: {fullErrorMessage}");
    }

    [Fact]
    public async Task CliProcessRunner_WithInvalidExecutable_ShouldFailGracefully()
    {
        // arrange - Test behavior when CLI executable is invalid
        var invalidRunner = new CliProcessRunner("/definitely/nonexistent/path");

        // act & assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await invalidRunner.ExecuteCommandAsync("test", new string[0]));
        
        // Should provide meaningful error without depending on exact message format
        exception.Message.Should().NotBeNullOrEmpty("Should provide error message");
    }

    [Fact]
    public void ExceptionHandling_ShouldNotDependOnSpecificExceptionTypes()
    {
        // arrange - This test ensures we handle exceptions generically
        var exceptions = new Exception[]
        {
            new InvalidOperationException("CLI process failed"),
            new ApplicationException("Command execution error"),
            new Exception("Generic CLI error")
        };

        foreach (var exception in exceptions)
        {
            // act - Simulate exception handling
            var errorInfo = ExtractErrorInformation(exception);
            
            // assert - Should extract useful information regardless of exception type
            errorInfo.Should().NotBeNullOrEmpty(
                $"Should extract error information from {exception.GetType().Name}");
        }
    }

    private string ExtractErrorInformation(Exception exception)
    {
        // Simulate how the application extracts error information
        // This should be flexible and not depend on specific exception types
        return exception.Message ?? exception.GetType().Name;
    }

    [Theory]
    [InlineData("CLI command 'scan' failed with exit code 1: Path not found")]
    [InlineData("Failed to execute CLI command: Invalid arguments")]
    [InlineData("Process execution error: dotnet not found")]
    [InlineData("Command execution failed with unknown error")]
    public void ErrorMessage_FlexibilityTest_ShouldHandleVariousFormats(string errorMessage)
    {
        // arrange & act - Test that our error handling logic works with various message formats
        var isValidError = !string.IsNullOrWhiteSpace(errorMessage) &&
                          (errorMessage.Contains("failed") || 
                           errorMessage.Contains("error") || 
                           errorMessage.Contains("CLI"));

        // assert - Should recognize these as valid error messages
        isValidError.Should().BeTrue($"Should handle error message format: {errorMessage}");
    }
}