using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using DocxTemplate.UI.Services;

namespace DocxTemplate.UI.ViewModels;

public class ProcessingResultsViewModel : StepViewModelBase
{
    private readonly ICliCommandService _cliCommandService;
    private string _processingStatus = "";
    private bool _isProcessing = false;
    private bool _isProcessingComplete = false;
    private bool _processingSuccessful = false;
    private string _processingResults = "";
    private string _logFilePath = "";
    private string _outputFolderPath = "";
    private string _templateSetName = "";
    private int _placeholderCount = 0;
    private Dictionary<string, string> _placeholderValues = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public ProcessingResultsViewModel(ICliCommandService cliCommandService)
    {
        _cliCommandService = cliCommandService ?? throw new ArgumentNullException(nameof(cliCommandService));

        var canProcessTemplates = this.WhenAnyValue(
            x => x.IsProcessing,
            x => x.IsProcessingComplete,
            x => x.TemplateSetName,
            x => x.OutputFolderPath,
            x => x.PlaceholderCount,
            (isProcessing, isComplete, templateName, outputPath, placeholderCount) => 
                !isProcessing && !isComplete && !string.IsNullOrEmpty(templateName) && 
                !string.IsNullOrEmpty(outputPath) && placeholderCount > 0);

        var canOpenFolder = this.WhenAnyValue(
            x => x.IsProcessingComplete,
            x => x.ProcessingSuccessful,
            x => x.OutputFolderPath,
            (isComplete, isSuccessful, outputPath) => 
                isComplete && isSuccessful && !string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath));

        var canOpenLog = this.WhenAnyValue(
            x => x.LogFilePath,
            logPath => !string.IsNullOrEmpty(logPath) && File.Exists(logPath));

        ProcessTemplatesCommand = ReactiveCommand.CreateFromTask(ProcessTemplatesAsync, canProcessTemplates);
        OpenFolderCommand = ReactiveCommand.Create(OpenFolder, canOpenFolder);
        OpenLogCommand = ReactiveCommand.Create(OpenLog, canOpenLog);
        StartOverCommand = ReactiveCommand.Create(StartOver);
    }

    public string ProcessingStatus
    {
        get => _processingStatus;
        set => this.RaiseAndSetIfChanged(ref _processingStatus, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
    }

    public bool IsProcessingComplete
    {
        get => _isProcessingComplete;
        set => this.RaiseAndSetIfChanged(ref _isProcessingComplete, value);
    }

    public bool ProcessingSuccessful
    {
        get => _processingSuccessful;
        set => this.RaiseAndSetIfChanged(ref _processingSuccessful, value);
    }

    public string ProcessingResults
    {
        get => _processingResults;
        set => this.RaiseAndSetIfChanged(ref _processingResults, value);
    }

    public string LogFilePath
    {
        get => _logFilePath;
        set => this.RaiseAndSetIfChanged(ref _logFilePath, value);
    }

    public string OutputFolderPath
    {
        get => _outputFolderPath;
        set => this.RaiseAndSetIfChanged(ref _outputFolderPath, value);
    }

    public string TemplateSetName
    {
        get => _templateSetName;
        set => this.RaiseAndSetIfChanged(ref _templateSetName, value);
    }

    public int PlaceholderCount
    {
        get => _placeholderCount;
        set => this.RaiseAndSetIfChanged(ref _placeholderCount, value);
    }

    public string ProcessingSummary => 
        $"Sada šablon: {TemplateSetName}\n" +
        $"Výstupní složka: {OutputFolderPath}\n" +
        $"Počet zástupných symbolů: {PlaceholderCount}";

    public ReactiveCommand<Unit, Unit> ProcessTemplatesCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLogCommand { get; }
    public ReactiveCommand<Unit, Unit> StartOverCommand { get; }

    public void SetProcessingData(string templateSetPath, string outputPath, Dictionary<string, string> placeholders)
    {
        // Extract just the directory name from the full path
        TemplateSetName = Path.GetFileName(templateSetPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? "Neznámá sada";
        OutputFolderPath = outputPath;
        _placeholderValues = placeholders ?? new Dictionary<string, string>();
        PlaceholderCount = _placeholderValues.Count;
        
        this.RaisePropertyChanged(nameof(ProcessingSummary));
    }

    private async Task ProcessTemplatesAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            IsProcessing = true;
            IsProcessingComplete = false;
            ProcessingSuccessful = false;
            
            // Create log file
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            LogFilePath = Path.Combine(Path.GetTempPath(), $"docx-processing-{timestamp}.log");
            
            using var logFile = new StreamWriter(LogFilePath, append: true);
            await logFile.WriteLineAsync($"=== Template Processing Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            
            // Step 1: Copy templates
            ProcessingStatus = "Kopírování šablon...";
            await logFile.WriteLineAsync("Phase 1: Copying templates");
            
            var copyResult = await ExecuteCopyCommand(logFile);
            if (!copyResult.Success)
            {
                throw new InvalidOperationException($"Template copy failed: {copyResult.Error}");
            }
            
            // Step 2: Create temporary JSON mapping file
            var tempJsonPath = Path.Combine(Path.GetTempPath(), $"placeholder-map-{timestamp}.json");
            try
            {
                await CreatePlaceholderMappingFile(tempJsonPath, logFile);
                
                // Step 3: Replace placeholders
                ProcessingStatus = "Nahrazování zástupných symbolů...";
                await logFile.WriteLineAsync("Phase 2: Replacing placeholders");
                
                var replaceResult = await ExecuteReplaceCommand(tempJsonPath, logFile);
                if (!replaceResult.Success)
                {
                    throw new InvalidOperationException($"Placeholder replacement failed: {replaceResult.Error}");
                }
                
                // Success
                ProcessingSuccessful = true;
                ProcessingResults = $"Zpracováno {PlaceholderCount} zástupných symbolů v sadě '{TemplateSetName}'";
                ProcessingStatus = "Zpracování dokončeno úspěšně";
                
                await logFile.WriteLineAsync("=== Processing completed successfully ===");
            }
            finally
            {
                // Clean up temporary JSON file
                if (File.Exists(tempJsonPath))
                {
                    try
                    {
                        File.Delete(tempJsonPath);
                        await logFile.WriteLineAsync($"Cleaned up temporary file: {tempJsonPath}");
                    }
                    catch (Exception ex)
                    {
                        await logFile.WriteLineAsync($"Warning: Could not delete temporary file {tempJsonPath}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ProcessingSuccessful = false;
            ProcessingResults = $"Chyba při zpracování: {ex.Message}";
            ProcessingStatus = "Zpracování selhalo";
            
            using var logFile = new StreamWriter(LogFilePath, append: true);
            await logFile.WriteLineAsync($"=== Processing failed: {ex.Message} ===");
            await logFile.WriteLineAsync($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            IsProcessing = false;
            IsProcessingComplete = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task<(bool Success, string Error)> ExecuteCopyCommand(StreamWriter logFile)
    {
        try
        {
            var templateSetPath = Path.GetDirectoryName(TemplateSetName) ?? "";
            var arguments = new[]
            {
                "--source", $"\"{templateSetPath}\"",
                "--target", $"\"{OutputFolderPath}\"",
                "--format", "json"
            };

            var result = await _cliCommandService.ExecuteCommandAsync("copy", arguments);
            await logFile.WriteLineAsync($"Copy command output: {result}");
            return (true, "");
        }
        catch (Exception ex)
        {
            await logFile.WriteLineAsync($"Copy command failed: {ex.Message}");
            return (false, ex.Message);
        }
    }

    private async Task CreatePlaceholderMappingFile(string filePath, StreamWriter logFile)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var jsonContent = JsonSerializer.Serialize(_placeholderValues, jsonOptions);
        await File.WriteAllTextAsync(filePath, jsonContent);
        
        await logFile.WriteLineAsync($"Created placeholder mapping file: {filePath}");
        await logFile.WriteLineAsync($"Mapping content: {jsonContent}");
    }

    private async Task<(bool Success, string Error)> ExecuteReplaceCommand(string mappingFilePath, StreamWriter logFile)
    {
        try
        {
            var arguments = new[]
            {
                "--folder", $"\"{OutputFolderPath}\"",
                "--map", $"\"{mappingFilePath}\"",
                "--format", "json"
            };

            var result = await _cliCommandService.ExecuteCommandAsync("replace", arguments);
            await logFile.WriteLineAsync($"Replace command output: {result}");
            return (true, "");
        }
        catch (Exception ex)
        {
            await logFile.WriteLineAsync($"Replace command failed: {ex.Message}");
            return (false, ex.Message);
        }
    }

    private void OpenFolder()
    {
        if (!Directory.Exists(OutputFolderPath))
            return;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = OutputFolderPath
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            ProcessingResults = $"Nelze otevřít složku: {ex.Message}";
        }
    }

    private void OpenLog()
    {
        if (!File.Exists(LogFilePath))
            return;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = LogFilePath
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            ProcessingResults = $"Nelze otevřít log: {ex.Message}";
        }
    }

    private void StartOver()
    {
        // Reset all processing state
        IsProcessing = false;
        IsProcessingComplete = false;
        ProcessingSuccessful = false;
        ProcessingStatus = "";
        ProcessingResults = "";
        LogFilePath = "";
        
        // Clear processing data but keep template info for next run
        // Don't reset TemplateSetName, OutputFolderPath, PlaceholderCount
        // as these are used for validation and may be needed for retry
        
        // Request navigation back to step 1
        // This will be handled by the WizardViewModel
        RequestNavigationToStep?.Invoke(1);
    }

    public event Action<int>? RequestNavigationToStep;

    public override bool ValidateStep()
    {
        return !string.IsNullOrEmpty(TemplateSetName) && 
               !string.IsNullOrEmpty(OutputFolderPath) && 
               PlaceholderCount > 0;
    }
}