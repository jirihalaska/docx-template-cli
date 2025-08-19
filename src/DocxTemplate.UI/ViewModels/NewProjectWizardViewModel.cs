using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Views.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// Wizard ViewModel for New Project workflow
/// </summary>
public class NewProjectWizardViewModel : WizardViewModelBase
{
    private TemplateSetSelectionViewModel _templateSelectionViewModel = null!;
    private PlaceholderDiscoveryViewModel _placeholderDiscoveryViewModel = null!;
    private PlaceholderInputViewModel _placeholderInputViewModel = null!;
    private OutputFolderSelectionViewModel _outputFolderSelectionViewModel = null!;
    private ProcessingResultsViewModel _processingResultsViewModel = null!;

    public NewProjectWizardViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override int TotalSteps => 5;

    public override string NextButtonText
    {
        get
        {
            return CurrentStep switch
            {
                4 => "Generuj", // Step 4: Processing step
                _ => "Další"
            };
        }
    }

    protected override void InitializeSteps()
    {
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte sadu šablon", IsActive = true },        // Step 0: Template Selection
            new() { Title = "Nalezené zástupné symboly" },                   // Step 1: Placeholder Discovery  
            new() { Title = "Zadání hodnot zástupných symbolů" },            // Step 2: Placeholder Input  
            new() { Title = "Výběr výstupní složky" },                       // Step 3: Output Folder Selection
            new() { Title = "Zpracování a výsledky" }                        // Step 4: Processing Results
        };
        Steps = new ReadOnlyCollection<StepInfo>(stepList);
    }

    protected override void InitializeViewModels()
    {
        // Step 0: Template Selection
        _templateSelectionViewModel = ServiceProvider.GetRequiredService<TemplateSetSelectionViewModel>();
        _templateSelectionViewModel.TemplateSelected += () => {
            // Auto-advance when template is selected
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (CurrentStep == 0 && _templateSelectionViewModel.SelectedTemplateSet != null)
                {
                    GoToNextStep();
                }
            });
        };

        // Step 1: Placeholder Discovery
        _placeholderDiscoveryViewModel = ServiceProvider.GetRequiredService<PlaceholderDiscoveryViewModel>();

        // Step 2: Placeholder Input
        _placeholderInputViewModel = ServiceProvider.GetRequiredService<PlaceholderInputViewModel>();

        // Step 3: Output Folder Selection
        _outputFolderSelectionViewModel = ServiceProvider.GetRequiredService<OutputFolderSelectionViewModel>();

        // Step 4: Processing Results
        _processingResultsViewModel = ServiceProvider.GetRequiredService<ProcessingResultsViewModel>();
        _processingResultsViewModel.RequestNavigationToStep += (step) => {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                CurrentStep = Math.Max(0, Math.Min(step, TotalSteps - 1));
                UpdateStepStates(CurrentStep);
            });
        };

        StepViewModels = new List<StepViewModelBase>
        {
            _templateSelectionViewModel,     // Step 0
            _placeholderDiscoveryViewModel,  // Step 1
            _placeholderInputViewModel,      // Step 2
            _outputFolderSelectionViewModel, // Step 3
            _processingResultsViewModel      // Step 4
        };
    }

    protected override void InitializeViews()
    {
        var step0View = new Step1TemplateSelectionView
        {
            DataContext = _templateSelectionViewModel
        };

        var step1View = new Step2PlaceholderDiscoveryView
        {
            DataContext = _placeholderDiscoveryViewModel
        };

        var step2View = new Step3PlaceholderInputView
        {
            DataContext = _placeholderInputViewModel
        };

        var step3View = new Step4OutputSelectionView
        {
            DataContext = _outputFolderSelectionViewModel
        };

        var step4View = new Step5ProcessingResultsView
        {
            DataContext = _processingResultsViewModel
        };

        StepViews = new List<UserControl>
        {
            step0View,  // Step 0: Template Selection
            step1View,  // Step 1: Placeholder Discovery
            step2View,  // Step 2: Placeholder Input
            step3View,  // Step 3: Output Folder Selection
            step4View   // Step 4: Processing Results
        };
    }

    protected override void PrepareStepTransition(int fromStep, int toStep)
    {
        switch (toStep)
        {
            case 1: // Moving to Placeholder Discovery
                if (_templateSelectionViewModel.SelectedTemplateSet != null)
                {
                    _placeholderDiscoveryViewModel.SetSelectedTemplateSet(_templateSelectionViewModel.SelectedTemplateSet);
                    
                    // Trigger placeholder scanning
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                        await _placeholderDiscoveryViewModel.ScanPlaceholdersAsync();
                    });
                }
                break;

            case 2: // Moving to Placeholder Input
                var discoveredPlaceholders = _placeholderDiscoveryViewModel.DiscoveredPlaceholders;
                _placeholderInputViewModel.SetDiscoveredPlaceholders(discoveredPlaceholders);
                break;

            case 4: // Moving to Processing
                if (_templateSelectionViewModel.SelectedTemplateSet != null && 
                    !string.IsNullOrEmpty(_outputFolderSelectionViewModel.SelectedFolderPath))
                {
                    _processingResultsViewModel.SetProcessingData(
                        _templateSelectionViewModel.SelectedTemplateSet.TemplateSetInfo.Path,
                        _outputFolderSelectionViewModel.SelectedFolderPath,
                        _placeholderInputViewModel.GetReplacementMapping(),
                        ProcessingMode.NewProject);
                        
                    // Auto-start processing
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                        _processingResultsViewModel.ProcessTemplatesCommand.Execute().Subscribe();
                    });
                }
                break;
        }
    }

    protected override void OnWizardCompleted()
    {
        // New project wizard completion logic
        // Could show summary or navigate to results
    }

    /// <summary>
    /// Get the selected template set (for external access)
    /// </summary>
    public TemplateSetItemViewModel? SelectedTemplateSet => _templateSelectionViewModel.SelectedTemplateSet;

    /// <summary>
    /// Get the discovered placeholders (for external access)
    /// </summary>
    public IEnumerable<PlaceholderItemViewModel> DiscoveredPlaceholders => _placeholderDiscoveryViewModel.DiscoveredPlaceholders;

    /// <summary>
    /// Get the placeholder replacement mappings (for external access)
    /// </summary>
    public Dictionary<string, string> PlaceholderMappings => _placeholderInputViewModel.GetReplacementMapping();
}