using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Platform.Storage;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Step 4: Output Folder Selection
/// </summary>
public class OutputFolderSelectionViewModel : StepViewModelBase
{
    private string? _selectedFolderPath;
    private string? _validationMessage;
    private bool _isSelectingFolder;

    public OutputFolderSelectionViewModel()
    {
        SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderAsync);
        
        // Subscribe to property changes to update validation
        this.WhenAnyValue(x => x.SelectedFolderPath)
            .Subscribe(_ => ValidateStep());
    }

    /// <summary>
    /// Path to the selected output folder
    /// </summary>
    public string? SelectedFolderPath
    {
        get => _selectedFolderPath;
        set => this.RaiseAndSetIfChanged(ref _selectedFolderPath, value);
    }

    /// <summary>
    /// Validation message to display to the user
    /// </summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => this.RaiseAndSetIfChanged(ref _validationMessage, value);
    }

    /// <summary>
    /// Whether a folder selection dialog is currently open
    /// </summary>
    public bool IsSelectingFolder
    {
        get => _isSelectingFolder;
        private set => this.RaiseAndSetIfChanged(ref _isSelectingFolder, value);
    }

    /// <summary>
    /// Indicates whether a folder has been selected
    /// </summary>
    public bool HasSelectedFolder => !string.IsNullOrEmpty(SelectedFolderPath);

    /// <summary>
    /// Display text for the selected folder path
    /// </summary>
    public string FolderDisplayText => HasSelectedFolder 
        ? SelectedFolderPath! 
        : "Žádná složka není vybrána";

    /// <summary>
    /// Command to open folder selection dialog
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }

    /// <summary>
    /// Step title for display
    /// </summary>
    public string StepTitle => "Výběr výstupní složky";

    /// <summary>
    /// Step description for display
    /// </summary>
    public string StepDescription => "Vyberte složku, kde budou uloženy zpracované dokumenty.";

    /// <summary>
    /// Opens a folder picker dialog to select output folder
    /// </summary>
    private async Task SelectFolderAsync()
    {
        try
        {
            IsSelectingFolder = true;
            
            // Get the current main window's storage provider
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;

            if (mainWindow?.StorageProvider == null)
            {
                ValidationMessage = "Chyba: Nelze otevřít dialog pro výběr složky.";
                return;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = "Vyberte výstupní složku",
                AllowMultiple = false
            };

            var result = await mainWindow.StorageProvider.OpenFolderPickerAsync(options);
            
            if (result?.Count > 0)
            {
                var selectedFolder = result[0];
                SelectedFolderPath = selectedFolder.Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Chyba při výběru složky: {ex.Message}";
        }
        finally
        {
            IsSelectingFolder = false;
        }
    }

    /// <inheritdoc />
    public override bool ValidateStep()
    {
        ValidationMessage = null;
        
        if (string.IsNullOrWhiteSpace(SelectedFolderPath))
        {
            IsValid = false;
            ValidationError = "Musíte vybrat výstupní složku.";
            return false;
        }

        if (!Directory.Exists(SelectedFolderPath))
        {
            IsValid = false;
            ValidationError = "Vybraná složka neexistuje.";
            ValidationMessage = "Vybraná složka neexistuje nebo není dostupná.";
            return false;
        }

        // Test write permissions
        try
        {
            var testFile = Path.Combine(SelectedFolderPath, $".temp_write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            
            IsValid = true;
            ValidationError = null;
            ValidationMessage = "Složka je dostupná a zapisovatelná.";
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            IsValid = false;
            ValidationError = "Nemáte oprávnění k zápisu do vybrané složky.";
            ValidationMessage = "Nemáte oprávnění k zápisu do vybrané složky. Vyberte jinou složku.";
            return false;
        }
        catch (Exception ex)
        {
            IsValid = false;
            ValidationError = "Chyba při ověření složky.";
            ValidationMessage = $"Chyba při ověření složky: {ex.Message}";
            return false;
        }
    }

    /// <inheritdoc />
    public override void OnStepActivated()
    {
        base.OnStepActivated();
        // Refresh property notifications when step is activated
        this.RaisePropertyChanged(nameof(HasSelectedFolder));
        this.RaisePropertyChanged(nameof(FolderDisplayText));
    }
}