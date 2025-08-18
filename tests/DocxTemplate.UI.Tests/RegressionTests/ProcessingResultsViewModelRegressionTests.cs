using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocxTemplate.UI.Tests.RegressionTests;

/// <summary>
/// Regression tests for ProcessingResultsViewModel to prevent critical path management bugs
/// </summary>
public class ProcessingResultsViewModelRegressionTests
{
    private readonly Mock<ICliCommandService> _mockCliCommandService;
    private readonly ProcessingResultsViewModel _viewModel;

    public ProcessingResultsViewModelRegressionTests()
    {
        _mockCliCommandService = new Mock<ICliCommandService>();
        _viewModel = new ProcessingResultsViewModel(_mockCliCommandService.Object);
    }

    [Fact]
    public void SetProcessingData_WithFullPath_ShouldStorePathCorrectlyForCliCommands()
    {
        // arrange - This tests the critical bug where full path was lost
        var fullTemplatePath = Path.Combine("C:", "Users", "test", "templates", "MyTemplateSet");
        var outputPath = Path.Combine("C:", "temp", "output");
        var placeholders = new Dictionary<string, string> { { "test", "value" } };
        
        // act
        _viewModel.SetProcessingData(fullTemplatePath, outputPath, placeholders);
        
        // assert - Display name should be just the directory name
        _viewModel.TemplateSetName.Should().Be("MyTemplateSet", "Display name should be just the directory name");
        
        // assert - Internal path should be stored for CLI usage
        _viewModel.OutputFolderPath.Should().Be(outputPath);
        _viewModel.PlaceholderCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessTemplatesAsync_WithStoredPath_ShouldPassCorrectPathToCopyCommand()
    {
        // arrange - Set up the scenario that was failing
        var fullTemplatePath = "/Users/test/templates/TestSet";
        var outputPath = "/tmp/output";
        var placeholders = new Dictionary<string, string> { { "company", "Test Corp" } };
        
        // Mock successful CLI commands
        _mockCliCommandService.Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy completed successfully")
            .Callback<string, string[]>((command, args) =>
            {
                // Verify the copy command receives the full path, not just directory name
                args.Should().Contain($"\"{fullTemplatePath}\"", 
                    "Copy command should receive the full template path that was stored");
            });
            
        _mockCliCommandService.Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync("Replace completed successfully");
        
        _viewModel.SetProcessingData(fullTemplatePath, outputPath, placeholders);
        
        // act - This should not fail with "sourcePath cannot be null or empty"
        await _viewModel.ProcessTemplatesCommand.Execute().FirstAsync();
        
        // assert - Processing should complete successfully
        _viewModel.ProcessingSuccessful.Should().BeTrue("Processing should complete without path errors");
        _viewModel.IsProcessingComplete.Should().BeTrue();
        
        // Verify copy command was called with correct arguments
        _mockCliCommandService.Verify(x => x.ExecuteCommandAsync("copy", 
            It.Is<string[]>(args => 
                args.Contains("--source") && 
                args.Contains($"\"{fullTemplatePath}\"") &&
                args.Contains("--target") &&
                args.Contains($"\"{outputPath}\"")
            )), Times.Once, "Copy command should be called with full template path and output path");
    }

    [Fact]
    public void SetProcessingData_WithNullPath_ShouldHandleGracefully()
    {
        // arrange - Edge case that could cause issues
        string? nullPath = null;
        var outputPath = "/tmp/output";
        var placeholders = new Dictionary<string, string>();
        
        // act - Should not throw
        _viewModel.SetProcessingData(nullPath, outputPath, placeholders);
        
        // assert - Should handle null gracefully
        _viewModel.TemplateSetName.Should().Be("Neznámá sada", "Should show default name for null path");
    }

    [Fact]
    public void SetProcessingData_WithEmptyPath_ShouldHandleGracefully()
    {
        // arrange - Edge case that could cause issues
        var emptyPath = "";
        var outputPath = "/tmp/output";
        var placeholders = new Dictionary<string, string>();
        
        // act - Should not throw
        _viewModel.SetProcessingData(emptyPath, outputPath, placeholders);
        
        // assert - Should handle empty string gracefully (Path.GetFileName("") returns "")
        _viewModel.TemplateSetName.Should().BeEmpty("Path.GetFileName of empty string returns empty string");
    }

    [Fact]
    public void SetProcessingData_WithPathEndingInSeparators_ShouldNormalizeCorrectly()
    {
        // arrange - Test path normalization that was part of the original bug
        var pathWithSeparators = "/Users/test/templates/TestSet///";
        var outputPath = "/tmp/output";
        var placeholders = new Dictionary<string, string>();
        
        // act
        _viewModel.SetProcessingData(pathWithSeparators, outputPath, placeholders);
        
        // assert - Should extract clean directory name
        _viewModel.TemplateSetName.Should().Be("TestSet", "Should extract clean directory name ignoring trailing separators");
    }

    [Theory]
    [InlineData("/Users/test/templates/My Template Set")]
    [InlineData("C:\\Users\\test\\templates\\My Template Set")]
    [InlineData("/Users/test/templates/Set With Spaces")]
    public void SetProcessingData_WithPathsContainingSpaces_ShouldExtractNameCorrectly(string pathWithSpaces)
    {
        // arrange - Paths with spaces were problematic in CLI commands
        var outputPath = "/tmp/output";
        var placeholders = new Dictionary<string, string>();
        
        // act
        _viewModel.SetProcessingData(pathWithSpaces, outputPath, placeholders);
        
        // assert - Should extract the directory name correctly
        var expectedName = Path.GetFileName(pathWithSpaces.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        _viewModel.TemplateSetName.Should().Be(expectedName, "Should extract directory name from paths with spaces");
    }

    [Fact]
    public async Task ProcessTemplatesAsync_WhenCopyCommandFails_ShouldNotCallReplaceCommand()
    {
        // arrange - Test error handling doesn't mask path issues
        var fullTemplatePath = "/Users/test/templates/TestSet";
        var outputPath = "/tmp/output";
        var placeholders = new Dictionary<string, string> { { "test", "value" } };
        
        _mockCliCommandService.Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ThrowsAsync(new InvalidOperationException("Copy failed due to path issue"));
        
        _viewModel.SetProcessingData(fullTemplatePath, outputPath, placeholders);
        
        // act
        await _viewModel.ProcessTemplatesCommand.Execute().FirstAsync();
        
        // assert - Should fail gracefully and not call replace
        _viewModel.ProcessingSuccessful.Should().BeFalse("Processing should fail when copy fails");
        _viewModel.ProcessingResults.Should().Contain("Copy failed due to path issue");
        
        // Verify replace was never called
        _mockCliCommandService.Verify(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()), 
            Times.Never, "Replace command should not be called when copy fails");
    }
}