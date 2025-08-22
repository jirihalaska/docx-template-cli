using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DocxTemplate.Core.Models;
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
    /// <returns>JSON string containing only filled placeholders in expected nested format</returns>
    public string GetReplacementMappingJson()
    {
        var mapping = GetReplacementMapping();
        var wrappedMapping = new { placeholders = mapping };
        return JsonSerializer.Serialize(wrappedMapping, new JsonSerializerOptions
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
    private string _selectedImagePath = string.Empty;

    public PlaceholderInputItemViewModel(PlaceholderItemViewModel placeholderItem)
    {
        PlaceholderItem = placeholderItem ?? throw new ArgumentNullException(nameof(placeholderItem));
        
        // Initialize commands
        SelectImageCommand = ReactiveCommand.CreateFromTask(SelectImageAsync, 
            this.WhenAnyValue(x => x.IsImagePlaceholder));
        ClearImageCommand = ReactiveCommand.Create(ClearImageSelection,
            this.WhenAnyValue(x => x.IsImagePlaceholder, x => x.SelectedImagePath, 
                (isImage, imagePath) => isImage && !string.IsNullOrWhiteSpace(imagePath)));
        
        // Initialize the filled state based on current input value (should be empty initially)
        IsFilled = !string.IsNullOrWhiteSpace(_inputValue);
        
        // Subscribe to input value changes for automatic whitespace normalization
        this.WhenAnyValue(x => x.InputValue)
            .Subscribe(value => 
            {
                if (!IsImagePlaceholder)
                {
                    var normalized = NormalizeWhitespace(value);
                    
                    // Always update IsFilled state first, based on the normalized value
                    IsFilled = !string.IsNullOrWhiteSpace(normalized);
                    
                    // Then normalize the input if needed (this might trigger the subscription again,
                    // but IsFilled is already set correctly above)
                    if (normalized != value)
                    {
                        InputValue = normalized;
                    }
                }
            });
            
        // Subscribe to image path changes
        this.WhenAnyValue(x => x.SelectedImagePath)
            .Subscribe(imagePath =>
            {
                if (IsImagePlaceholder)
                {
                    IsFilled = !string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath);
                    // For image placeholders, the input value is the file path
                    if (IsFilled)
                    {
                        InputValue = imagePath;
                    }
                }
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
    /// Indicates whether this is an image placeholder
    /// </summary>
    public bool IsImagePlaceholder => PlaceholderItem.Placeholder.Type == PlaceholderType.Image;

    /// <summary>
    /// Selected image file path for image placeholders
    /// </summary>
    public string SelectedImagePath
    {
        get => _selectedImagePath;
        private set => this.RaiseAndSetIfChanged(ref _selectedImagePath, value);
    }

    /// <summary>
    /// Indicates whether an image has been selected
    /// </summary>
    public bool HasImageSelected => !string.IsNullOrWhiteSpace(SelectedImagePath);

    /// <summary>
    /// Display name of the selected image file
    /// </summary>
    public string SelectedImageFileName => HasImageSelected ? Path.GetFileName(SelectedImagePath) : string.Empty;

    /// <summary>
    /// Command to select an image file
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectImageCommand { get; }

    /// <summary>
    /// Command to clear the selected image
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearImageCommand { get; }

    /// <summary>
    /// Maximum width for the image placeholder
    /// </summary>
    public int? MaxWidth => PlaceholderItem.Placeholder.ImageProperties?.MaxWidth;

    /// <summary>
    /// Maximum height for the image placeholder
    /// </summary>
    public int? MaxHeight => PlaceholderItem.Placeholder.ImageProperties?.MaxHeight;

    /// <summary>
    /// Image dimensions display text
    /// </summary>
    public string ImageDimensionsText => IsImagePlaceholder && MaxWidth.HasValue && MaxHeight.HasValue 
        ? $"Maximální rozměry: {MaxWidth}×{MaxHeight} pixelů" 
        : string.Empty;

    /// <summary>
    /// Clears the input value and resets the filled state
    /// </summary>
    public void ClearValue()
    {
        InputValue = string.Empty;
        if (IsImagePlaceholder)
        {
            SelectedImagePath = string.Empty;
        }
    }

    /// <summary>
    /// Selects an image file using the system file picker
    /// </summary>
    private async Task SelectImageAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime switch
            {
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
                _ => null
            };

            if (topLevel?.StorageProvider == null)
                return;

            var fileTypeFilters = new List<FilePickerFileType>
            {
                new("Image Files")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp" },
                    MimeTypes = new[] { "image/png", "image/jpeg", "image/gif", "image/bmp" }
                },
                FilePickerFileTypes.All
            };

            var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vyberte obrázek",
                AllowMultiple = false,
                FileTypeFilter = fileTypeFilters
            });

            if (result?.Count > 0)
            {
                var selectedFile = result[0];
                var localPath = selectedFile.TryGetLocalPath();
                
                if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                {
                    // Validate image file
                    if (IsValidImageFile(localPath))
                    {
                        SelectedImagePath = localPath;
                    }
                    else
                    {
                        // TODO: Show error message to user
                        // For now, just don't set the path
                    }
                }
            }
        }
        catch (Exception)
        {
            // TODO: Log error and show user-friendly message
            // For now, silently ignore
        }
    }

    /// <summary>
    /// Clears the selected image
    /// </summary>
    private void ClearImageSelection()
    {
        SelectedImagePath = string.Empty;
        InputValue = string.Empty;
    }

    /// <summary>
    /// Validates that the selected file is a supported image format
    /// </summary>
    private static bool IsValidImageFile(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
            return supportedExtensions.Contains(extension);
        }
        catch
        {
            return false;
        }
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