using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Platform.Storage;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Mode 2 Step 1: Existing Project Folder Selection
/// Selects a folder that already contains partially processed templates
/// </summary>
public class ExistingProjectFolderSelectionViewModel : StepViewModelBase
{
    private string? _selectedFolderPath;
    private string? _validationMessage;
    private bool _isSelectingFolder;
    private int _docxFileCount;

    public ExistingProjectFolderSelectionViewModel()
    {
        SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderAsync);
        
        // Subscribe to property changes to update validation
        this.WhenAnyValue(x => x.SelectedFolderPath)
            .Subscribe(_ => UpdateValidation());
    }

    /// <summary>
    /// Path to the selected existing project folder
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
    /// Number of .docx files found in the selected folder
    /// </summary>
    public int DocxFileCount
    {
        get => _docxFileCount;
        private set => this.RaiseAndSetIfChanged(ref _docxFileCount, value);
    }

    /// <summary>
    /// Display text showing the number of .docx files found
    /// </summary>
    public string DocxFileCountText => DocxFileCount == 0 
        ? "Nenalezeny žádné .docx soubory"
        : DocxFileCount == 1 
            ? "Nalezen 1 .docx soubor"
            : $"Nalezeno {DocxFileCount} .docx souborů";

    /// <summary>
    /// Czech text for folder selection instruction
    /// </summary>
    public string FolderSelectionText => "Vyberte výstupní složku";

    /// <summary>
    /// Czech text for folder description
    /// </summary>
    public string FolderDescriptionText => "Složka s částečně zpracovanými šablonami (včetně podsložek)";

    /// <summary>
    /// Command to open folder selection dialog
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }

    public override bool ValidateStep()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderPath))
        {
            ValidationMessage = "Prosím vyberte složku";
            IsValid = false;
            return false;
        }

        if (!Directory.Exists(SelectedFolderPath))
        {
            ValidationMessage = "Vybraná složka neexistuje";
            IsValid = false;
            return false;
        }

        // Count .docx files in the folder and all subfolders recursively
        try
        {
            var docxFiles = Directory.GetFiles(SelectedFolderPath, "*.docx", SearchOption.AllDirectories);
            DocxFileCount = docxFiles.Length;

            if (DocxFileCount == 0)
            {
                ValidationMessage = "Ve vybrané složce a jejích podsložkách nejsou žádné .docx soubory";
                IsValid = false;
                return false;
            }

            ValidationMessage = null;
            IsValid = true;
            return true;
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Chyba při čtení složky: {ex.Message}";
            IsValid = false;
            return false;
        }
    }

    private async Task SelectFolderAsync()
    {
        IsSelectingFolder = true;

        try
        {
            // Get the main window's storage provider
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime 
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                
            if (topLevel?.MainWindow == null)
            {
                ValidationMessage = "Nelze otevřít dialog pro výběr složky";
                return;
            }

            var storage = topLevel.MainWindow.StorageProvider;

            var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Vyberte složku s částečně zpracovanými šablonami",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                SelectedFolderPath = result[0].Path.LocalPath;
                this.RaisePropertyChanged(nameof(DocxFileCountText));
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
}