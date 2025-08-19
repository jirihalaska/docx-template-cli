using System;
using System.Reactive;
using ReactiveUI;
using DocxTemplate.UI.Models;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// ViewModel for Step 0: Processing Mode Selection
/// </summary>
public class ModeSelectionViewModel : StepViewModelBase
{
    private ProcessingMode _selectedMode = ProcessingMode.NewProject;

    public ModeSelectionViewModel()
    {
        SelectModeCommand = ReactiveCommand.Create<ProcessingMode>(OnModeSelected);

        // Always valid - user can select any mode
        IsValid = true;
    }

    /// <summary>
    /// Currently selected processing mode
    /// </summary>
    public ProcessingMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMode, value);
            UpdateValidation();

            // Raise event to notify wizard of mode selection
            ModeSelected?.Invoke(value);
        }
    }

    /// <summary>
    /// Command to select a processing mode
    /// </summary>
    public ReactiveCommand<ProcessingMode, Unit> SelectModeCommand { get; }

    /// <summary>
    /// Event raised when a mode is selected
    /// </summary>
    public event Action<ProcessingMode>? ModeSelected;

    /// <summary>
    /// Czech text for "Select processing mode"
    /// </summary>
    public string SelectModeText => "Co chcete dělat?";

    /// <summary>
    /// Czech text for "New Project" mode
    /// </summary>
    public string NewProjectTitle => "Nová zakázka";

    /// <summary>
    /// Czech description for "New Project" mode
    /// </summary>
    public string NewProjectDescription => "Zpracování nových šablon";

    /// <summary>
    /// Czech details for "New Project" mode
    /// </summary>
    public string NewProjectDetails => "• Výběr sady šablon\n• Kompletní workflow";

    /// <summary>
    /// Czech text for "Update Project" mode
    /// </summary>
    public string UpdateProjectTitle => "Úprava zakázky";

    /// <summary>
    /// Czech description for "Update Project" mode
    /// </summary>
    public string UpdateProjectDescription => "Dokončení částečně vyplněných";

    /// <summary>
    /// Czech details for "Update Project" mode
    /// </summary>
    public string UpdateProjectDetails => "• Výběr výstupní složky\n• Doplnění chybějících hodnot";

    /// <summary>
    /// Returns whether New Project mode is selected
    /// </summary>
    public bool IsNewProjectSelected => SelectedMode == ProcessingMode.NewProject;

    /// <summary>
    /// Returns whether Update Project mode is selected
    /// </summary>
    public bool IsUpdateProjectSelected => SelectedMode == ProcessingMode.UpdateProject;

    public override bool ValidateStep()
    {
        // Mode selection is always valid
        IsValid = true;
        ValidationError = null;
        return true;
    }

    private void OnModeSelected(ProcessingMode mode)
    {
        SelectedMode = mode;

        // Raise property change notifications for selection state
        this.RaisePropertyChanged(nameof(IsNewProjectSelected));
        this.RaisePropertyChanged(nameof(IsUpdateProjectSelected));
    }
}
