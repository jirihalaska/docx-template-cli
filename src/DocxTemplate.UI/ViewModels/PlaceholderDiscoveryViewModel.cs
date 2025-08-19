using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Services;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Step 2: Placeholder Discovery & Display
/// </summary>
public class PlaceholderDiscoveryViewModel : StepViewModelBase
{
    private readonly IPlaceholderScanService _placeholderScanService;
    
    private ObservableCollection<PlaceholderItemViewModel> _discoveredPlaceholders;
    private bool _isScanning;
    private bool _hasScanCompleted;
    private bool _hasScanError;
    private string _scanStatusMessage = string.Empty;
    private string _scanErrorMessage = string.Empty;
    private int _totalPlaceholdersFound;
    private int _totalOccurrences;
    private TemplateSetItemViewModel? _selectedTemplateSet;

    public PlaceholderDiscoveryViewModel(IPlaceholderScanService placeholderScanService)
    {
        _placeholderScanService = placeholderScanService ?? throw new ArgumentNullException(nameof(placeholderScanService));
        
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
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasScanError = true;
                ScanErrorMessage = "Není vybrána žádná sada šablon. Vraťte se zpět a vyberte šablony.";
                UpdateValidation();
            });
            return;
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsScanning = true;
            HasScanCompleted = false;
            HasScanError = false;
            ScanStatusMessage = "Prohledávání šablon...";
            ScanErrorMessage = string.Empty;
            UpdateValidation();
        });

        try
        {
            var templatePath = SelectedTemplateSet.TemplateSetInfo.Path;
            
            var scanResult = await _placeholderScanService.ScanPlaceholdersAsync(
                templatePath,
                pattern: @"\{\{.*?\}\}",
                recursive: true,
                cancellationToken: CancellationToken.None);

            await ProcessScanResult(scanResult);
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasScanError = true;
                ScanErrorMessage = $"Chyba při prohledávání šablon: {ex.Message}";
                ScanStatusMessage = "Prohledávání selhalo";
            });
        }
        finally
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsScanning = false;
                UpdateValidation();
            });
        }
    }

    /// <summary>
    /// Processes the scan result and updates the UI
    /// </summary>
    private async Task ProcessScanResult(PlaceholderScanResult scanResult)
    {
        // Process data in background thread
        var processedData = await Task.Run(() =>
        {
            // Create list starting with discovered placeholders (already sorted by scan service)
            var placeholderViewModels = scanResult.Placeholders
                .Select(p => new PlaceholderItemViewModel(p))
                .ToList();

            // Ensure SOUBOR_PREFIX appears first, even if not found in templates
            if (!scanResult.Placeholders.Any(p => p.Name == Placeholder.FilePrefixPlaceholder))
            {
                // Create a placeholder for SOUBOR_PREFIX with zero occurrences
                var souborPrefixPlaceholder = new Placeholder
                {
                    Name = Placeholder.FilePrefixPlaceholder,
                    Pattern = @"\{\{.*?\}\}",
                    Locations = new List<PlaceholderLocation>().AsReadOnly(),
                    TotalOccurrences = 0
                };
                
                // Insert at the beginning of the list
                placeholderViewModels.Insert(0, new PlaceholderItemViewModel(souborPrefixPlaceholder));
            }

            var statusMessage = scanResult.IsSuccessful
                ? $"Dokončeno: nalezeno {scanResult.UniquePlaceholderCount} zástupných symbolů ({scanResult.TotalOccurrences} výskytů)"
                : null;
            
            var errorMessage = !scanResult.IsSuccessful
                ? $"Prohledávání dokončeno s chybami: {string.Join(", ", scanResult.Errors.Select(e => e.DisplayMessage))}"
                : null;

            return new
            {
                Placeholders = placeholderViewModels,
                TotalPlaceholders = scanResult.UniquePlaceholderCount,
                TotalOccurrences = scanResult.TotalOccurrences,
                IsSuccessful = scanResult.IsSuccessful,
                StatusMessage = statusMessage,
                ErrorMessage = errorMessage
            };
        });

        // Update UI properties on UI thread
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            DiscoveredPlaceholders.Clear();
            foreach (var placeholder in processedData.Placeholders)
            {
                DiscoveredPlaceholders.Add(placeholder);
            }

            TotalPlaceholdersFound = processedData.TotalPlaceholders;
            TotalOccurrences = processedData.TotalOccurrences;
            
            if (processedData.IsSuccessful)
            {
                HasScanCompleted = true;
                ScanStatusMessage = processedData.StatusMessage;
            }
            else
            {
                HasScanError = true;
                ScanErrorMessage = processedData.ErrorMessage;
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
    public string DisplayText 
    {
        get
        {
            if (Name == Placeholder.FilePrefixPlaceholder && OccurrenceCount == 0)
            {
                return $"{Name} (systémový prefix - volitelný)";
            }
            return $"{Name} ({OccurrenceCount} výskytů)";
        }
    }

    /// <summary>
    /// List of files where this placeholder appears
    /// </summary>
    public string FilesListText
    {
        get
        {
            if (Name == Placeholder.FilePrefixPlaceholder && !Placeholder.Locations.Any())
            {
                return "Automaticky přidáno pro prefix souborů";
            }
            return string.Join(", ", 
                Placeholder.Locations.Select(l => System.IO.Path.GetFileName(l.FilePath)).Distinct());
        }
    }

    /// <summary>
    /// Number of files containing this placeholder
    /// </summary>
    public int FileCount => Placeholder.Locations.Select(l => l.FilePath).Distinct().Count();
}