using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Step 1: Template Set Selection
/// </summary>
public class TemplateSetSelectionViewModel : StepViewModelBase
{
    private readonly ITemplateSetDiscoveryService _templateSetDiscoveryService;
    private readonly string _templatesPath = Path.Combine(AppContext.BaseDirectory, "templates");

    private ObservableCollection<TemplateSetItemViewModel> _templateSets;
    private TemplateSetItemViewModel? _selectedTemplateSet;
    private bool _isLoading;
    private bool _hasError;
    private string _errorText = string.Empty;

    public TemplateSetSelectionViewModel(ITemplateSetDiscoveryService templateSetDiscoveryService)
    {
        _templateSetDiscoveryService = templateSetDiscoveryService ??
            throw new ArgumentNullException(nameof(templateSetDiscoveryService));

        _templateSets = new ObservableCollection<TemplateSetItemViewModel>();

        // Initialize reactive command for template set selection
        SelectTemplateSetCommand = ReactiveCommand.Create<TemplateSetItemViewModel>(OnTemplateSetSelected);
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadTemplateSetsAsync);
    }

    /// <summary>
    /// Collection of available template sets
    /// </summary>
    public ObservableCollection<TemplateSetItemViewModel> TemplateSets
    {
        get => _templateSets;
    }

    /// <summary>
    /// Currently selected template set
    /// </summary>
    public TemplateSetItemViewModel? SelectedTemplateSet
    {
        get => _selectedTemplateSet;
        set
        {
            // If we're already on UI thread, update directly
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                this.RaiseAndSetIfChanged(ref _selectedTemplateSet, value);
                UpdateValidation();
            }
            else
            {
                // Post to UI thread if we're on a different thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    this.RaiseAndSetIfChanged(ref _selectedTemplateSet, value);
                    UpdateValidation();
                });
            }
        }
    }

    /// <summary>
    /// Indicates if template sets are being loaded
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Indicates if there was an error loading template sets
    /// </summary>
    public bool HasError
    {
        get => _hasError;
        private set => this.RaiseAndSetIfChanged(ref _hasError, value);
    }

    /// <summary>
    /// Error text to display when HasError is true
    /// </summary>
    public string ErrorText
    {
        get => _errorText;
        private set => this.RaiseAndSetIfChanged(ref _errorText, value);
    }

    /// <summary>
    /// Command to select a template set
    /// </summary>
    public ReactiveCommand<TemplateSetItemViewModel, Unit> SelectTemplateSetCommand { get; }

    /// <summary>
    /// Command to refresh the template sets list
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// Step title for display
    /// </summary>
    public string StepTitle => "Vyberte sadu šablon";

    /// <summary>
    /// Step description for display
    /// </summary>
    public string StepDescription => "Kterou předlohu (druh řízení) chcete zvolit?";

    /// <inheritdoc />
    public override bool ValidateStep()
    {
        if (SelectedTemplateSet != null)
        {
            IsValid = true;
            ErrorMessage = string.Empty;
            return true;
        }

        IsValid = false;
        ErrorMessage = "Musíte vybrat sadu šablon pro pokračování.";
        return false;
    }

    /// <inheritdoc />
    public override void OnStepActivated()
    {
        base.OnStepActivated();

        // Load template sets when step is activated
        // Run on UI thread to avoid cross-thread issues with ObservableCollection
        _ = LoadTemplateSetsAsync();
    }

    /// <summary>
    /// Loads template sets from the templates directory
    /// </summary>
    public async Task LoadTemplateSetsAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorText = string.Empty;

        try
        {
            var templateSets = await _templateSetDiscoveryService.DiscoverTemplateSetsAsync(
                _templatesPath,
                CancellationToken.None);

            TemplateSets.Clear();

            if (!templateSets.Any())
            {
                HasError = true;
                ErrorText = "Ve složce ./templates nebyly nalezeny žádné šablony. " +
                           "Zkontrolujte prosím, že složka existuje a obsahuje podsložky se šablonami Word.";
            }
            else
            {
                // Sort template sets alphabetically by name
                var sortedTemplateSets = templateSets.OrderBy(ts => ts.Name, StringComparer.OrdinalIgnoreCase);

                foreach (var templateSet in sortedTemplateSets)
                {
                    var viewModel = new TemplateSetItemViewModel(templateSet);
                    TemplateSets.Add(viewModel);
                }
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorText = $"Chyba při načítání šablon: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        UpdateValidation();
    }

    /// <summary>
    /// Event raised when a template set is selected to trigger auto-advance
    /// </summary>
    public event Action? TemplateSelected;

    /// <summary>
    /// Handles template set selection
    /// </summary>
    /// <param name="templateSet">Selected template set</param>
    private void OnTemplateSetSelected(TemplateSetItemViewModel templateSet)
    {
        // If we're already on UI thread, execute directly
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            // Deselect all other template sets
            foreach (var ts in TemplateSets)
            {
                ts.IsSelected = false;
            }

            // Select the chosen template set
            templateSet.IsSelected = true;
            SelectedTemplateSet = templateSet;

            // Trigger auto-advance to next step
            TemplateSelected?.Invoke();
        }
        else
        {
            // Post to UI thread if we're on a different thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() => OnTemplateSetSelected(templateSet));
        }
    }
}

/// <summary>
/// ViewModel for individual template set items
/// </summary>
public class TemplateSetItemViewModel : ReactiveObject
{
    private bool _isSelected;

    public TemplateSetItemViewModel(TemplateSetInfo templateSetInfo)
    {
        TemplateSetInfo = templateSetInfo ?? throw new ArgumentNullException(nameof(templateSetInfo));
    }

    /// <summary>
    /// Template set information
    /// </summary>
    public TemplateSetInfo TemplateSetInfo { get; }

    /// <summary>
    /// Name of the template set
    /// </summary>
    public string Name => TemplateSetInfo.Name;

    /// <summary>
    /// Number of files in the template set
    /// </summary>
    public int FileCount => TemplateSetInfo.FileCount;

    /// <summary>
    /// Formatted display text with name and file count
    /// </summary>
    public string DisplayText => $"{Name} ({FileCount} souborů)";

    /// <summary>
    /// Total size formatted for display
    /// </summary>
    public string TotalSizeFormatted => TemplateSetInfo.TotalSizeFormatted;

    /// <summary>
    /// Indicates if this template set is selected
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
}
