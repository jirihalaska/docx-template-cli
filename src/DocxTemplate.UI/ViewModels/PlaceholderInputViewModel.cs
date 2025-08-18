using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Text.RegularExpressions;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Step 3: Placeholder Value Input
/// </summary>
public class PlaceholderInputViewModel : StepViewModelBase
{
    private ObservableCollection<PlaceholderInputItemViewModel> _placeholderInputs;
    private int _filledPlaceholdersCount;

    public PlaceholderInputViewModel()
    {
        _placeholderInputs = new ObservableCollection<PlaceholderInputItemViewModel>();
        
        ClearAllCommand = ReactiveCommand.Create(ClearAllInputs);
    }

    /// <summary>
    /// Collection of placeholder input items for dynamic form generation
    /// </summary>
    public ObservableCollection<PlaceholderInputItemViewModel> PlaceholderInputs
    {
        get => _placeholderInputs;
        private set => this.RaiseAndSetIfChanged(ref _placeholderInputs, value);
    }

    /// <summary>
    /// Number of placeholders that have been filled with values
    /// </summary>
    public int FilledPlaceholdersCount
    {
        get => _filledPlaceholdersCount;
        private set => this.RaiseAndSetIfChanged(ref _filledPlaceholdersCount, value);
    }

    /// <summary>
    /// Total number of placeholders discovered
    /// </summary>
    public int TotalPlaceholdersCount => PlaceholderInputs.Count;

    /// <summary>
    /// Summary text showing completion status
    /// </summary>
    public string CompletionSummary => $"{FilledPlaceholdersCount} z {TotalPlaceholdersCount} zástupných symbolů vyplněno";

    /// <summary>
    /// Command to clear all input values
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }

    /// <summary>
    /// Step title for display
    /// </summary>
    public string StepTitle => "Zadání hodnot zástupných symbolů";

    /// <summary>
    /// Step description for display
    /// </summary>
    public string StepDescription => "Zadejte hodnoty pro nalezené zástupné symboly.";

    /// <summary>
    /// Warning text explaining unfilled placeholders
    /// </summary>
    public string UnfilledWarningText => "Nevyplněné zástupné symboly zůstanou v dokumentech ve formátu {{PLACEHOLDER}}.";

    /// <inheritdoc />
    public override bool ValidateStep()
    {
        // Step 3 allows progression regardless of completion status (no required fields)
        IsValid = true;
        ErrorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Sets the discovered placeholders from Step 2
    /// </summary>
    /// <param name="discoveredPlaceholders">Placeholder items from Step 2 discovery</param>
    public void SetDiscoveredPlaceholders(IEnumerable<PlaceholderItemViewModel> discoveredPlaceholders)
    {
        if (discoveredPlaceholders == null)
        {
            PlaceholderInputs.Clear();
            UpdateCompletionCounts();
            return;
        }

        // Create input items for each discovered placeholder  
        // Maintain same order as Step 2 (no additional sorting)
        var inputItems = discoveredPlaceholders
            .Select(placeholder => new PlaceholderInputItemViewModel(placeholder))
            .ToList();

        // Subscribe to value changes for completion tracking
        foreach (var inputItem in inputItems)
        {
            inputItem.WhenAnyValue(x => x.IsFilled)
                .Subscribe(_ => UpdateCompletionCounts());
        }

        PlaceholderInputs.Clear();
        foreach (var item in inputItems)
        {
            PlaceholderInputs.Add(item);
        }

        UpdateCompletionCounts();
        UpdateValidation();
    }

    /// <summary>
    /// Clears all input values and resets visual states
    /// </summary>
    private void ClearAllInputs()
    {
        foreach (var input in PlaceholderInputs)
        {
            input.ClearValue();
        }
        UpdateCompletionCounts();
    }

    /// <summary>
    /// Updates the completion counts and summary
    /// </summary>
    private void UpdateCompletionCounts()
    {
        FilledPlaceholdersCount = PlaceholderInputs.Count(p => p.IsFilled);
        this.RaisePropertyChanged(nameof(TotalPlaceholdersCount));
        this.RaisePropertyChanged(nameof(CompletionSummary));
    }

    /// <summary>
    /// Gets the JSON mapping for filled placeholders only
    /// </summary>
    /// <returns>JSON object containing only filled placeholders</returns>
    public Dictionary<string, string> GetReplacementMapping()
    {
        return PlaceholderInputs
            .Where(p => p.IsFilled)
            .ToDictionary(p => p.PlaceholderName, p => p.InputValue);
    }

    /// <summary>
    /// Gets the JSON mapping as a JSON string for CLI replace command
    /// </summary>
    /// <returns>JSON string containing only filled placeholders</returns>
    public string GetReplacementMappingJson()
    {
        var mapping = GetReplacementMapping();
        return JsonSerializer.Serialize(mapping, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

/// <summary>
/// ViewModel for individual placeholder input items
/// </summary>
public class PlaceholderInputItemViewModel : ReactiveObject
{
    private string _inputValue = string.Empty;
    private bool _isFilled;

    public PlaceholderInputItemViewModel(PlaceholderItemViewModel placeholderItem)
    {
        PlaceholderItem = placeholderItem ?? throw new ArgumentNullException(nameof(placeholderItem));
        
        // Subscribe to input value changes for automatic whitespace normalization
        this.WhenAnyValue(x => x.InputValue)
            .Subscribe(value => 
            {
                var normalized = NormalizeWhitespace(value);
                if (normalized != value)
                {
                    InputValue = normalized;
                }
                IsFilled = !string.IsNullOrWhiteSpace(normalized);
            });
    }

    /// <summary>
    /// The original placeholder item from Step 2
    /// </summary>
    public PlaceholderItemViewModel PlaceholderItem { get; }

    /// <summary>
    /// Name of the placeholder (e.g., "NÁZEV_FIRMY")
    /// </summary>
    public string PlaceholderName => PlaceholderItem.Name;

    /// <summary>
    /// Display label for the input field (e.g., "NÁZEV_FIRMY:")
    /// </summary>
    public string DisplayLabel => $"{PlaceholderName}:";

    /// <summary>
    /// Number of occurrences of this placeholder
    /// </summary>
    public int OccurrenceCount => PlaceholderItem.OccurrenceCount;

    /// <summary>
    /// Files where this placeholder appears
    /// </summary>
    public string FilesListText => PlaceholderItem.FilesListText;

    /// <summary>
    /// User input value for this placeholder
    /// </summary>
    public string InputValue
    {
        get => _inputValue;
        set => this.RaiseAndSetIfChanged(ref _inputValue, value ?? string.Empty);
    }

    /// <summary>
    /// Indicates whether this placeholder has been filled with a value
    /// </summary>
    public bool IsFilled
    {
        get => _isFilled;
        private set => this.RaiseAndSetIfChanged(ref _isFilled, value);
    }

    /// <summary>
    /// Indicates whether this placeholder is unfilled (for warning styling)
    /// </summary>
    public bool IsUnfilled => !IsFilled;

    /// <summary>
    /// Clears the input value and resets the filled state
    /// </summary>
    public void ClearValue()
    {
        InputValue = string.Empty;
    }

    /// <summary>
    /// Normalizes whitespace in the input (newlines, tabs, multiple spaces → single space)
    /// </summary>
    /// <param name="input">Original input text</param>
    /// <returns>Normalized text with single spaces</returns>
    private static string NormalizeWhitespace(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Replace newlines, tabs, and multiple spaces with single space
        return Regex.Replace(input.Trim(), @"\s+", " ");
    }
}