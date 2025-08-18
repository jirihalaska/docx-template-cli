using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DocxTemplate.UI.ViewModels;
using DocxTemplate.UI.Services;

namespace DocxTemplate.UI.Tests.ViewModels;

public class ProcessingResultsViewModelTests
{
    private readonly Mock<ICliCommandService> _mockCliCommandService;
    private readonly ProcessingResultsViewModel _viewModel;

    public ProcessingResultsViewModelTests()
    {
        _mockCliCommandService = new Mock<ICliCommandService>();
        _viewModel = new ProcessingResultsViewModel(_mockCliCommandService.Object);
    }

    [Fact]
    public void Constructor_WithNullCliCommandService_ThrowsArgumentNullException()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => new ProcessingResultsViewModel(null!));
    }

    [Fact]
    public void Constructor_WithValidService_InitializesProperties()
    {
        // arrange
        var cliService = new Mock<ICliCommandService>();

        // act
        var viewModel = new ProcessingResultsViewModel(cliService.Object);

        // assert
        Assert.False(viewModel.IsProcessing);
        Assert.False(viewModel.IsProcessingComplete);
        Assert.False(viewModel.ProcessingSuccessful);
        Assert.Equal("", viewModel.ProcessingStatus);
        Assert.Equal("", viewModel.ProcessingResults);
        Assert.Equal("", viewModel.LogFilePath);
        Assert.Equal("", viewModel.OutputFolderPath);
        Assert.Equal("", viewModel.TemplateSetName);
        Assert.Equal(0, viewModel.PlaceholderCount);
    }

    [Fact]
    public void SetProcessingData_WithValidData_UpdatesProperties()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string>
        {
            { "{{name}}", "John Doe" },
            { "{{date}}", "2025-01-18" }
        };

        // act
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        // assert
        Assert.Equal("templates", _viewModel.TemplateSetName);
        Assert.Equal(outputPath, _viewModel.OutputFolderPath);
        Assert.Equal(2, _viewModel.PlaceholderCount);
        Assert.Contains("templates", _viewModel.ProcessingSummary);
        Assert.Contains(outputPath, _viewModel.ProcessingSummary);
        Assert.Contains("2", _viewModel.ProcessingSummary);
    }

    [Fact]
    public void SetProcessingData_WithNullPlaceholders_HandlesGracefully()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";

        // act
        _viewModel.SetProcessingData(templateSetPath, outputPath, null!);

        // assert
        Assert.Equal("templates", _viewModel.TemplateSetName);
        Assert.Equal(outputPath, _viewModel.OutputFolderPath);
        Assert.Equal(0, _viewModel.PlaceholderCount);
    }

    [Fact]
    public void ValidateStep_WithCompleteData_ReturnsTrue()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{test}}", "value" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        // act
        var isValid = _viewModel.ValidateStep();

        // assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStep_WithMissingData_ReturnsFalse()
    {
        // act
        var isValid = _viewModel.ValidateStep();

        // assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ProcessTemplatesCommand_WithSuccessfulCopyAndReplace_CompletesSuccessfully()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy successful");
        
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync("Replace successful");

        // act
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // assert
        Assert.True(_viewModel.IsProcessingComplete);
        Assert.True(_viewModel.ProcessingSuccessful);
        Assert.Contains("templates", _viewModel.ProcessingResults);
        Assert.Contains("dokončeno úspěšně", _viewModel.ProcessingStatus);
        Assert.False(string.IsNullOrEmpty(_viewModel.LogFilePath));
    }

    [Fact]
    public async Task ProcessTemplatesCommand_WithCopyFailure_HandlesError()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ThrowsAsync(new InvalidOperationException("Copy failed"));

        // act
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // assert
        Assert.True(_viewModel.IsProcessingComplete);
        Assert.False(_viewModel.ProcessingSuccessful);
        Assert.Contains("Chyba při zpracování", _viewModel.ProcessingResults);
        Assert.Contains("selhalo", _viewModel.ProcessingStatus);
        Assert.False(string.IsNullOrEmpty(_viewModel.LogFilePath));
    }

    [Fact]
    public async Task ProcessTemplatesCommand_WithReplaceFailure_HandlesError()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy successful");
        
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ThrowsAsync(new InvalidOperationException("Replace failed"));

        // act
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // assert
        Assert.True(_viewModel.IsProcessingComplete);
        Assert.False(_viewModel.ProcessingSuccessful);
        Assert.Contains("Chyba při zpracování", _viewModel.ProcessingResults);
        Assert.Contains("selhalo", _viewModel.ProcessingStatus);
    }

    [Fact]
    public void ProcessTemplatesCommand_CanExecute_WhenValidDataAndNotProcessing()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        // act
        var canExecute = _viewModel.ProcessTemplatesCommand.CanExecute.FirstAsync().Wait();

        // assert
        Assert.True(canExecute);
    }

    [Fact]
    public async Task ProcessTemplatesCommand_CannotExecute_WhenProcessing()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);
        
        // Start processing to set IsProcessing to true
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .Returns(Task.Delay(1000).ContinueWith(_ => "Copy in progress"));
        
        var processingTask = _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // Wait a bit to let processing start
        await Task.Delay(50);
        
        // Ignore the processing task for this test
        _ = processingTask;

        // act
        var canExecute = await _viewModel.ProcessTemplatesCommand.CanExecute.FirstAsync();

        // assert
        Assert.False(canExecute);
        
        // Cancel the long-running task
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy complete");
    }

    [Fact]
    public async Task OpenFolderCommand_CanExecute_WhenProcessingSuccessfulAndFolderExists()
    {
        // arrange
        var tempFolder = Path.GetTempPath();
        _viewModel.SetProcessingData("/test/templates", tempFolder, 
            new Dictionary<string, string> { { "{{test}}", "value" } });
        
        // Complete a successful processing to set the right state
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy successful");
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync("Replace successful");
            
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // act
        var canExecute = await _viewModel.OpenFolderCommand.CanExecute.FirstAsync();

        // assert
        Assert.True(canExecute);
    }

    [Fact]
    public async Task OpenLogCommand_CanExecute_WhenLogFileExists()
    {
        // arrange - Complete a processing to create a log file
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{test}}", "value" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy successful");
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync("Replace successful");
            
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        try
        {
            // act
            var canExecute = await _viewModel.OpenLogCommand.CanExecute.FirstAsync();

            // assert
            Assert.True(canExecute);
        }
        finally
        {
            // cleanup
            if (File.Exists(_viewModel.LogFilePath))
                File.Delete(_viewModel.LogFilePath);
        }
    }

    [Fact]
    public async Task StartOverCommand_Execute_ResetsStateAndRequestsNavigation()
    {
        // arrange
        var navigationRequested = false;
        var requestedStep = 0;
        _viewModel.RequestNavigationToStep += (step) => 
        { 
            navigationRequested = true; 
            requestedStep = step;
        };

        // Complete a processing first to set the state properly
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{test}}", "value" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy successful");
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync("Replace successful");
            
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // act
        await _viewModel.StartOverCommand.Execute(Unit.Default);

        // assert
        Assert.False(_viewModel.IsProcessing);
        Assert.False(_viewModel.IsProcessingComplete);
        Assert.False(_viewModel.ProcessingSuccessful);
        Assert.Equal("", _viewModel.ProcessingStatus);
        Assert.Equal("", _viewModel.ProcessingResults);
        Assert.True(navigationRequested);
        Assert.Equal(1, requestedStep);
    }

    [Fact]
    public async Task ProcessTemplatesCommand_CreatesLogFileWithTimestamp()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync("Copy successful");
        
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync("Replace successful");

        // act
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // assert
        Assert.False(string.IsNullOrEmpty(_viewModel.LogFilePath));
        Assert.Contains("docx-processing-", _viewModel.LogFilePath);
        Assert.Contains(".log", _viewModel.LogFilePath);
        
        // cleanup
        if (File.Exists(_viewModel.LogFilePath))
            File.Delete(_viewModel.LogFilePath);
    }

    [Fact]
    public async Task ProcessTemplatesCommand_CapturesCliOutput_InLogFile()
    {
        // arrange
        var templateSetPath = "/test/templates";
        var outputPath = "/test/output";
        var placeholders = new Dictionary<string, string> { { "{{name}}", "Test" } };
        _viewModel.SetProcessingData(templateSetPath, outputPath, placeholders);

        var copyOutput = "Copy operation completed successfully";
        var replaceOutput = "Replace operation completed successfully";

        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("copy", It.IsAny<string[]>()))
            .ReturnsAsync(copyOutput);
        
        _mockCliCommandService
            .Setup(x => x.ExecuteCommandAsync("replace", It.IsAny<string[]>()))
            .ReturnsAsync(replaceOutput);

        // act
        await _viewModel.ProcessTemplatesCommand.Execute(Unit.Default);

        // assert
        Assert.True(File.Exists(_viewModel.LogFilePath));
        var logContent = await File.ReadAllTextAsync(_viewModel.LogFilePath);
        Assert.Contains(copyOutput, logContent);
        Assert.Contains(replaceOutput, logContent);
        Assert.Contains("Template Processing Started", logContent);
        Assert.Contains("Processing completed successfully", logContent);
        
        // cleanup
        if (File.Exists(_viewModel.LogFilePath))
            File.Delete(_viewModel.LogFilePath);
    }
}