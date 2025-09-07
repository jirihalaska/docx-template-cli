using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.UI.Models;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

public class ProcessingResultsViewModel : StepViewModelBase
{
    private readonly ITemplateCopyService _templateCopyService;
    private readonly IPlaceholderReplaceService _placeholderReplaceService;
    private string _processingStatus = "";
    private bool _isProcessing = false;
    private bool _isProcessingComplete = false;
    private bool _processingSuccessful = false;
    private string _processingResults = "";
    private string _logFilePath = "";
    private string _outputFolderPath = "";
    private string _templateSetName = "";
    private string _templateSetPath = "";
    private int _placeholderCount = 0;
    private Dictionary<string, string> _placeholderValues = new();
    private ProcessingMode _processingMode = ProcessingMode.NewProject;
    private CancellationTokenSource? _cancellationTokenSource;

    public ProcessingResultsViewModel(ITemplateCopyService templateCopyService, IPlaceholderReplaceService placeholderReplaceService)
    {
        _templateCopyService = templateCopyService ?? throw new ArgumentNullException(nameof(templateCopyService));
        _placeholderReplaceService = placeholderReplaceService ?? throw new ArgumentNullException(nameof(placeholderReplaceService));

        var canProcessTemplates = this.WhenAnyValue(
            x => x.IsProcessing,
            x => x.IsProcessingComplete,
            x => x.TemplateSetName,
            x => x.OutputFolderPath,
            (isProcessing, isComplete, templateName, outputPath) => 
                !isProcessing && !isComplete && !string.IsNullOrEmpty(templateName) && 
                !string.IsNullOrEmpty(outputPath));

        var canOpenFolder = this.WhenAnyValue(
            x => x.IsProcessingComplete,
            x => x.ProcessingSuccessful,
            x => x.ActualTargetFolderPath,
            (isComplete, isSuccessful, actualTargetPath) => 
                isComplete && isSuccessful && !string.IsNullOrEmpty(actualTargetPath) && Directory.Exists(actualTargetPath));

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

    public string ActualTargetFolderPath
    {
        get
        {
            // In UpdateProject mode, we process files in place, so target is the same as source
            if (_processingMode == ProcessingMode.UpdateProject)
                return _templateSetPath;
                
            if (string.IsNullOrEmpty(_templateSetPath) || string.IsNullOrEmpty(OutputFolderPath))
                return OutputFolderPath;
                
            var sourceDirectoryName = Path.GetFileName(_templateSetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return Path.Combine(OutputFolderPath, sourceDirectoryName);
        }
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

    public void SetProcessingData(string templateSetPath, string outputPath, Dictionary<string, string> placeholders, ProcessingMode processingMode = ProcessingMode.NewProject)
    {
        // Store the full path for CLI commands
        _templateSetPath = templateSetPath ?? "";
        // Extract just the directory name for display
        TemplateSetName = Path.GetFileName(templateSetPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? "Neznámá sada";
        OutputFolderPath = outputPath;
        _placeholderValues = placeholders ?? new Dictionary<string, string>();
        PlaceholderCount = _placeholderValues.Count;
        _processingMode = processingMode;
        
        this.RaisePropertyChanged(nameof(ProcessingSummary));
        this.RaisePropertyChanged(nameof(ActualTargetFolderPath));
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
            
            // Step 1: Copy templates (only for NewProject mode)
            if (_processingMode == ProcessingMode.NewProject)
            {
                ProcessingStatus = "Kopírování šablon...";
                await logFile.WriteLineAsync("Phase 1: Copying templates");
                
                var copyResult = await ExecuteCopyCommand(logFile);
                if (!copyResult.Success)
                {
                    throw new InvalidOperationException($"Template copy failed: {copyResult.Error}");
                }
            }
            else
            {
                await logFile.WriteLineAsync("Phase 1: Skipping template copy (UpdateProject mode - processing existing files in place)");
            }
            
            // Step 2: Create temporary JSON mapping file
            var tempJsonPath = Path.Combine(Path.GetTempPath(), $"placeholder-map-{timestamp}.json");
            try
            {
                await CreatePlaceholderMappingFile(tempJsonPath, logFile);
                
                // Step 2/3: Replace placeholders
                ProcessingStatus = "Nahrazování zástupných symbolů...";
                await logFile.WriteLineAsync($"Phase 2: Replacing placeholders (Mode: {_processingMode})");
                
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
            // Extract file prefix from placeholders (SOUBOR_PREFIX)
            _placeholderValues.TryGetValue(Placeholder.FilePrefixPlaceholder, out var filePrefix);
            
            var result = await _templateCopyService.CopyTemplatesAsync(
                _templateSetPath,
                OutputFolderPath,
                preserveStructure: true,
                overwrite: false,
                filePrefix: filePrefix,
                cancellationToken: _cancellationTokenSource?.Token ?? CancellationToken.None);

            await logFile.WriteLineAsync($"Copy operation completed successfully. Files copied: {result.FilesCount}, Total size: {result.TotalBytesCount} bytes");
            await logFile.WriteLineAsync($"Target directory: {ActualTargetFolderPath}");
            return (true, "");
        }
        catch (Exception ex)
        {
            await logFile.WriteLineAsync($"Copy operation failed: {ex.Message}");
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
            // Load the replacement mappings from JSON file
            var jsonContent = await File.ReadAllTextAsync(mappingFilePath);
            var replacementMap = ReplacementMap.FromJson(jsonContent);

            // For UpdateProject mode, process files in the source folder directly
            var targetPath = _processingMode == ProcessingMode.UpdateProject ? _templateSetPath : ActualTargetFolderPath;
            await logFile.WriteLineAsync($"Target path for replacement: {targetPath}");
            
            var result = await _placeholderReplaceService.ReplacePlaceholdersAsync(
                targetPath,
                replacementMap,
                createBackup: false,
                cancellationToken: _cancellationTokenSource?.Token ?? CancellationToken.None);

            await logFile.WriteLineAsync($"Replace operation completed successfully. Files processed: {result.FilesProcessed}, Placeholders replaced: {result.TotalReplacements}");
            if (result.HasErrors)
            {
                await logFile.WriteLineAsync($"Warnings/Errors: {string.Join(", ", result.AllErrors)}");
            }
            return (true, "");
        }
        catch (Exception ex)
        {
            await logFile.WriteLineAsync($"Replace operation failed: {ex.Message}");
            return (false, ex.Message);
        }
    }

    private void OpenFolder()
    {
        if (!Directory.Exists(ActualTargetFolderPath))
            return;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = ActualTargetFolderPath
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
               !string.IsNullOrEmpty(OutputFolderPath);
    }
}