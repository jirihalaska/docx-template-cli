using System;
using System.Reactive;
using Avalonia.Controls;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Views.Steps;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// Coordinates mode selection and manages switching between different wizard implementations
/// </summary>
public class WizardCoordinatorViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private bool _showModeSelection = true;
    private ProcessingMode _selectedMode = ProcessingMode.NewProject;
    private WizardViewModelBase? _currentWizard;
    private ModeSelectionViewModel _modeSelectionViewModel;

    public WizardCoordinatorViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // Initialize mode selection
        _modeSelectionViewModel = serviceProvider.GetRequiredService<ModeSelectionViewModel>();
        _modeSelectionViewModel.ModeSelected += OnModeSelected;
        
        // Commands
        BackToModeSelectionCommand = ReactiveCommand.Create(BackToModeSelection);
    }

    #region Properties

    /// <summary>
    /// Whether to show mode selection or the active wizard
    /// </summary>
    public bool ShowModeSelection
    {
        get => _showModeSelection;
        private set => this.RaiseAndSetIfChanged(ref _showModeSelection, value);
    }

    /// <summary>
    /// The currently selected processing mode
    /// </summary>
    public ProcessingMode SelectedMode
    {
        get => _selectedMode;
        private set => this.RaiseAndSetIfChanged(ref _selectedMode, value);
    }

    /// <summary>
    /// The currently active wizard ViewModel
    /// </summary>
    public WizardViewModelBase? CurrentWizard
    {
        get => _currentWizard;
        private set => this.RaiseAndSetIfChanged(ref _currentWizard, value);
    }

    /// <summary>
    /// The mode selection ViewModel
    /// </summary>
    public ModeSelectionViewModel ModeSelectionViewModel => _modeSelectionViewModel;

    /// <summary>
    /// The mode selection view
    /// </summary>
    public UserControl ModeSelectionView { get; } = new Step0ModeSelectionView();

    /// <summary>
    /// Command to return to mode selection
    /// </summary>
    public ReactiveCommand<Unit, Unit> BackToModeSelectionCommand { get; }

    /// <summary>
    /// Title for the current state
    /// </summary>
    public string Title
    {
        get
        {
            if (ShowModeSelection)
                return "Vyberte režim zpracování";
            
            return SelectedMode switch
            {
                ProcessingMode.NewProject => "Nový projekt",
                ProcessingMode.UpdateProject => "Aktualizace projektu",
                _ => "Procesor šablon DOCX"
            };
        }
    }

    #endregion

    #region Event Handlers

    private void OnModeSelected(ProcessingMode mode)
    {
        SelectedMode = mode;
        CreateWizardForMode(mode);
        ShowModeSelection = false;
        
        // Update title
        this.RaisePropertyChanged(nameof(Title));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Create and initialize the appropriate wizard for the selected mode
    /// </summary>
    private void CreateWizardForMode(ProcessingMode mode)
    {
        CurrentWizard = mode switch
        {
            ProcessingMode.NewProject => new NewProjectWizardViewModel(_serviceProvider),
            ProcessingMode.UpdateProject => new UpdateProjectWizardViewModel(_serviceProvider),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown processing mode")
        };
    }

    /// <summary>
    /// Return to mode selection
    /// </summary>
    private void BackToModeSelection()
    {
        // Clean up current wizard
        CurrentWizard = null;
        
        // Reset mode selection
        _modeSelectionViewModel.SelectedMode = ProcessingMode.NewProject;
        ShowModeSelection = true;
        
        // Update title
        this.RaisePropertyChanged(nameof(Title));
    }

    /// <summary>
    /// Get status text for the main window
    /// </summary>
    public string GetStatusText()
    {
        if (ShowModeSelection)
            return "Vyberte režim zpracování šablon";

        if (CurrentWizard == null)
            return "Připraveno";

        return SelectedMode switch
        {
            ProcessingMode.NewProject => $"Nový projekt - {CurrentWizard.CurrentStepTitle}",
            ProcessingMode.UpdateProject => $"Aktualizace projektu - {CurrentWizard.CurrentStepTitle}",
            _ => "Zpracování šablon"
        };
    }

    #endregion
}