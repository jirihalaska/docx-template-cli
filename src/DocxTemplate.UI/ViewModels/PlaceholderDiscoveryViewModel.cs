using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.Core.Models;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Services;
using ReactiveUI;
using System.Text.Json;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Step 2: Placeholder Discovery & Display
/// </summary>
public class PlaceholderDiscoveryViewModel : StepViewModelBase
{
    private readonly ICliCommandService _cliCommandService;
    
    private ObservableCollection<PlaceholderItemViewModel> _discoveredPlaceholders;
    private bool _isScanning;
    private bool _hasScanCompleted;
    private bool _hasScanError;
    private string _scanStatusMessage = string.Empty;
    private string _scanErrorMessage = string.Empty;
    private int _totalPlaceholdersFound;
    private int _totalOccurrences;
    private TemplateSetItemViewModel? _selectedTemplateSet;

    public PlaceholderDiscoveryViewModel(ICliCommandService cliCommandService)
    {
        _cliCommandService = cliCommandService ?? throw new ArgumentNullException(nameof(cliCommandService));
        
        _discoveredPlaceholders = new ObservableCollection<PlaceholderItemViewModel>();
        
        RescanCommand = ReactiveCommand.CreateFromTask(ScanPlaceholdersAsync, 
            this.WhenAnyValue(x => x.IsScanning, scanning => !scanning));
    }

    /// <summary>
    /// Collection of discovered placeholders
    /// </summary>
    public ObservableCollection<PlaceholderItemViewModel> DiscoveredPlaceholders
    {
        get => _discoveredPlaceholders;
        private set => this.RaiseAndSetIfChanged(ref _discoveredPlaceholders, value);
    }

    /// <summary>
    /// Indicates if placeholder scanning is in progress
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        private set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }

    /// <summary>
    /// Indicates if scan has completed successfully
    /// </summary>
    public bool HasScanCompleted
    {
        get => _hasScanCompleted;
        private set => this.RaiseAndSetIfChanged(ref _hasScanCompleted, value);
    }

    /// <summary>
    /// Indicates if there was an error during scanning
    /// </summary>
    public bool HasScanError
    {
        get => _hasScanError;
        private set => this.RaiseAndSetIfChanged(ref _hasScanError, value);
    }

    /// <summary>
    /// Current scan status message
    /// </summary>
    public string ScanStatusMessage
    {
        get => _scanStatusMessage;
        private set => this.RaiseAndSetIfChanged(ref _scanStatusMessage, value);
    }

    /// <summary>
    /// Error message if scan failed
    /// </summary>
    public string ScanErrorMessage
    {
        get => _scanErrorMessage;
        private set => this.RaiseAndSetIfChanged(ref _scanErrorMessage, value);
    }

    /// <summary>
    /// Total number of unique placeholders found
    /// </summary>
    public int TotalPlaceholdersFound
    {
        get => _totalPlaceholdersFound;
        private set => this.RaiseAndSetIfChanged(ref _totalPlaceholdersFound, value);
    }

    /// <summary>
    /// Total number of placeholder occurrences found
    /// </summary>
    public int TotalOccurrences
    {
        get => _totalOccurrences;
        private set => this.RaiseAndSetIfChanged(ref _totalOccurrences, value);
    }

    /// <summary>
    /// Selected template set from Step 1
    /// </summary>
    public TemplateSetItemViewModel? SelectedTemplateSet
    {
        get => _selectedTemplateSet;
        set => this.RaiseAndSetIfChanged(ref _selectedTemplateSet, value);
    }

    /// <summary>
    /// Command to rescan placeholders
    /// </summary>
    public ReactiveCommand<Unit, Unit> RescanCommand { get; }

    /// <summary>
    /// Step title for display
    /// </summary>
    public string StepTitle => "Nalezení zástupných symbolů";

    /// <summary>
    /// Step description for display
    /// </summary>
    public string StepDescription => "Prohledávání šablon za účelem nalezení všech zástupných symbolů.";

    /// <inheritdoc />
    public override bool ValidateStep()
    {
        if (HasScanCompleted && !HasScanError)
        {
            IsValid = true;
            ErrorMessage = string.Empty;
            return true;
        }

        if (HasScanError)
        {
            IsValid = false;
            ErrorMessage = "Prohledávání šablon selhalo. Opravte chyby nebo se vraťte zpět a vyberte jiné šablony.";
            return false;
        }

        if (IsScanning)
        {
            IsValid = false;
            ErrorMessage = "Probíhá prohledávání šablon...";
            return false;
        }

        IsValid = false;
        ErrorMessage = "Prohledávání šablon nebylo dokončeno.";
        return false;
    }

    /// <inheritdoc />
    public override async void OnStepActivated()
    {
        base.OnStepActivated();
        
        // If we haven't scanned yet or if the template set has changed, start scanning
        if (!HasScanCompleted || !_discoveredPlaceholders.Any())
        {
            await ScanPlaceholdersAsync();
        }
    }

    /// <summary>
    /// Scans templates for placeholders using CLI command
    /// </summary>
    public async Task ScanPlaceholdersAsync()
    {
        if (SelectedTemplateSet == null)
        {
            HasScanError = true;
            ScanErrorMessage = "Není vybrána žádná sada šablon. Vraťte se zpět a vyberte šablony.";
            UpdateValidation();
            return;
        }

        IsScanning = true;
        HasScanCompleted = false;
        HasScanError = false;
        ScanStatusMessage = "Prohledávání šablon...";
        ScanErrorMessage = string.Empty;
        
        UpdateValidation();

        try
        {
            var templatePath = SelectedTemplateSet.TemplateSetInfo.Path;
            var arguments = new[] { "scan", "--path", $"\"{templatePath}\"", "--format", "json" };
            
            var jsonOutput = await _cliCommandService.ExecuteCommandAsync("", arguments);
            
            var scanResult = JsonSerializer.Deserialize<PlaceholderScanResult>(jsonOutput, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (scanResult != null)
            {
                await ProcessScanResult(scanResult);
            }
            else
            {
                throw new InvalidOperationException("Failed to parse scan results from CLI output");
            }
        }
        catch (Exception ex)
        {
            HasScanError = true;
            ScanErrorMessage = $"Chyba při prohledávání šablon: {ex.Message}";
            ScanStatusMessage = "Prohledávání selhalo";
        }
        finally
        {
            IsScanning = false;
            UpdateValidation();
        }
    }

    /// <summary>
    /// Processes the scan result and updates the UI
    /// </summary>
    private async Task ProcessScanResult(PlaceholderScanResult scanResult)
    {
        await Task.Run(() =>
        {
            DiscoveredPlaceholders.Clear();

            // Sort placeholders by discovery order (first occurrence determines position)
            // Use the first location as the reference point for ordering
            var sortedPlaceholders = scanResult.Placeholders
                .OrderBy(p => p.Locations.FirstOrDefault()?.FilePath ?? "")
                .ThenBy(p => p.Locations.FirstOrDefault()?.CharacterPositions?.FirstOrDefault() ?? 0)
                .ToList();

            foreach (var placeholder in sortedPlaceholders)
            {
                var viewModel = new PlaceholderItemViewModel(placeholder);
                DiscoveredPlaceholders.Add(viewModel);
            }

            TotalPlaceholdersFound = scanResult.UniquePlaceholderCount;
            TotalOccurrences = scanResult.TotalOccurrences;
            
            if (scanResult.IsSuccessful)
            {
                HasScanCompleted = true;
                ScanStatusMessage = $"Dokončeno: nalezeno {TotalPlaceholdersFound} zástupných symbolů ({TotalOccurrences} výskytů)";
            }
            else
            {
                HasScanError = true;
                var errorMessages = scanResult.Errors.Select(e => e.DisplayMessage).ToList();
                ScanErrorMessage = $"Prohledávání dokončeno s chybami: {string.Join(", ", errorMessages)}";
            }
        });
    }

    /// <summary>
    /// Sets the selected template set from Step 1
    /// </summary>
    public void SetSelectedTemplateSet(TemplateSetItemViewModel? templateSet)
    {
        if (SelectedTemplateSet != templateSet)
        {
            SelectedTemplateSet = templateSet;
            
            // Reset scan state when template set changes
            HasScanCompleted = false;
            HasScanError = false;
            DiscoveredPlaceholders.Clear();
            TotalPlaceholdersFound = 0;
            TotalOccurrences = 0;
            ScanStatusMessage = string.Empty;
            ScanErrorMessage = string.Empty;
            
            UpdateValidation();
        }
    }
}

/// <summary>
/// ViewModel for individual placeholder items in the discovery list
/// </summary>
public class PlaceholderItemViewModel : ReactiveObject
{
    public PlaceholderItemViewModel(Placeholder placeholder)
    {
        Placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));
    }

    /// <summary>
    /// The underlying placeholder model
    /// </summary>
    public Placeholder Placeholder { get; }

    /// <summary>
    /// Name of the placeholder (e.g., "NÁZEV_FIRMY")
    /// </summary>
    public string Name => Placeholder.Name;

    /// <summary>
    /// Number of times this placeholder appears across templates
    /// </summary>
    public int OccurrenceCount => Placeholder.TotalOccurrences;

    /// <summary>
    /// Formatted display text with occurrence count
    /// </summary>
    public string DisplayText => $"{Name} ({OccurrenceCount} výskytů)";

    /// <summary>
    /// List of files where this placeholder appears
    /// </summary>
    public string FilesListText => string.Join(", ", 
        Placeholder.Locations.Select(l => System.IO.Path.GetFileName(l.FilePath)).Distinct());

    /// <summary>
    /// Number of files containing this placeholder
    /// </summary>
    public int FileCount => Placeholder.Locations.Select(l => l.FilePath).Distinct().Count();
}