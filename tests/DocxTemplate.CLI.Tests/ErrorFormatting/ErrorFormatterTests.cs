using DocxTemplate.CLI.ErrorFormatting;
using DocxTemplate.Core.Models.Results;
using System.Text.Json;
using Xunit;

namespace DocxTemplate.CLI.Tests.ErrorFormatting;

public class ErrorFormatterTests
{
    [Fact]
    public void FormatForConsole_WithBasicError_ReturnsFormattedMessage()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "Validation",
            Message = "Invalid input format",
            OperationContext = "input validation",
            FilePath = "test.json",
            ExitCode = 1,
            IsCritical = false
        };

        // act
        var result = ErrorFormatter.FormatForConsole(error);

        // assert
        Assert.Contains("Error: Invalid input format", result);
        Assert.Contains("Operation: input validation", result);
        Assert.Contains("File: test.json", result);
        Assert.DoesNotContain("Technical Details:", result);
    }

    [Fact]
    public void FormatForConsole_WithDetails_IncludesTechnicalDetails()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "DocumentProcessing",
            Message = "Failed to process document",
            OperationContext = "document processing",
            Details = "StackTrace: at System.IO.File.ReadAllText...",
            ExitCode = 3,
            IsCritical = false
        };

        // act
        var result = ErrorFormatter.FormatForConsole(error, includeDetails: true);

        // assert
        Assert.Contains("Error: Failed to process document", result);
        Assert.Contains("Technical Details:", result);
        Assert.Contains("StackTrace: at System.IO.File.ReadAllText", result);
    }

    [Fact]
    public void FormatForConsole_WithoutDetails_DoesNotIncludeTechnicalDetails()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "FileAccess",
            Message = "Cannot access file",
            OperationContext = "file reading",
            Details = "Some technical details",
            ExitCode = 2,
            IsCritical = false
        };

        // act
        var result = ErrorFormatter.FormatForConsole(error, includeDetails: false);

        // assert
        Assert.Contains("Error: Cannot access file", result);
        Assert.DoesNotContain("Technical Details:", result);
        Assert.DoesNotContain("Some technical details", result);
    }

    [Fact]
    public void FormatAsJson_WithError_ReturnsValidJson()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "Validation",
            Message = "Invalid input",
            OperationContext = "validation",
            Details = "Detailed error info",
            FilePath = "input.json",
            ExitCode = 1,
            IsCritical = false
        };

        // act
        var json = ErrorFormatter.FormatAsJson(error);

        // assert
        Assert.True(IsValidJson(json));
        
        var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;
        
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Validation", root.GetProperty("error").GetProperty("type").GetString());
        Assert.Equal("Invalid input", root.GetProperty("error").GetProperty("message").GetString());
        Assert.Equal("validation", root.GetProperty("error").GetProperty("operation").GetString());
        Assert.Equal("input.json", root.GetProperty("error").GetProperty("filePath").GetString());
        Assert.Equal(1, root.GetProperty("error").GetProperty("exitCode").GetInt32());
        Assert.False(root.GetProperty("error").GetProperty("isCritical").GetBoolean());
    }

    [Fact]
    public void FormatCommandResult_WithSuccessfulResult_ReturnsSuccessMessage()
    {
        // arrange
        var result = CommandResult.Success("operation completed");

        // act
        var formatted = ErrorFormatter.FormatCommandResult(result);

        // assert
        Assert.Equal("Operation completed successfully.", formatted);
    }

    [Fact]
    public void FormatCommandResult_WithFailedResult_ReturnsErrorMessage()
    {
        // arrange
        var error = ErrorResult.ValidationError("Invalid data", "validation");
        var result = CommandResult.Failure<string>(error);

        // act
        var formatted = ErrorFormatter.FormatCommandResult(result);

        // assert
        Assert.Contains("Error: Invalid data", formatted);
        Assert.Contains("Operation: validation", formatted);
    }

    [Fact]
    public void FormatCommandResultAsJson_WithSuccessfulResult_ReturnsValidJson()
    {
        // arrange
        var data = new { name = "test", value = 42 };
        var result = CommandResult.Success(data);

        // act
        var json = ErrorFormatter.FormatCommandResultAsJson(result);

        // assert
        Assert.True(IsValidJson(json));
        
        var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;
        
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("test", root.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal(42, root.GetProperty("data").GetProperty("value").GetInt32());
    }

    [Fact]
    public void FormatCommandResultAsJson_WithFailedResult_ReturnsValidJson()
    {
        // arrange
        var error = ErrorResult.ValidationError("Invalid input", "validation");
        var result = CommandResult.Failure<string>(error);

        // act
        var json = ErrorFormatter.FormatCommandResultAsJson(result);

        // assert
        Assert.True(IsValidJson(json));
        
        var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;
        
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Validation", root.GetProperty("error").GetProperty("type").GetString());
        Assert.Equal("Invalid input", root.GetProperty("error").GetProperty("message").GetString());
    }

    [Fact]
    public void GetShortSummary_WithRegularError_ReturnsShortSummary()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "FileAccess",
            Message = "Cannot read file",
            OperationContext = "file reading",
            FilePath = "/path/to/document.docx",
            ExitCode = 2,
            IsCritical = false
        };

        // act
        var summary = ErrorFormatter.GetShortSummary(error);

        // assert
        Assert.Equal("ERROR: document.docx - Cannot read file", summary);
    }

    [Fact]
    public void GetShortSummary_WithCriticalError_ReturnsCriticalPrefix()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "Critical",
            Message = "System failure",
            OperationContext = "system operation",
            ExitCode = 99,
            IsCritical = true
        };

        // act
        var summary = ErrorFormatter.GetShortSummary(error);

        // assert
        Assert.Equal("CRITICAL: System failure", summary);
    }

    [Fact]
    public void GetShortSummary_WithoutFilePath_ReturnsMessageOnly()
    {
        // arrange
        var error = new ErrorResult
        {
            ErrorType = "Validation",
            Message = "Invalid configuration",
            OperationContext = "config validation",
            ExitCode = 1,
            IsCritical = false
        };

        // act
        var summary = ErrorFormatter.GetShortSummary(error);

        // assert
        Assert.Equal("ERROR: Invalid configuration", summary);
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}