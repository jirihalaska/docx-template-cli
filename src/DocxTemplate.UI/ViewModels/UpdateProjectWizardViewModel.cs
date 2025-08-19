using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.Views.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// Wizard ViewModel for Update Project workflow
/// </summary>
public class UpdateProjectWizardViewModel : WizardViewModelBase
{
    private ExistingProjectFolderSelectionViewModel _existingFolderSelectionViewModel = null!;
    private PlaceholderDiscoveryViewModel _placeholderDiscoveryViewModel = null!;
    private PlaceholderInputViewModel _placeholderInputViewModel = null!;
    private ProcessingResultsViewModel _processingResultsViewModel = null!;

    public UpdateProjectWizardViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override int TotalSteps => 4;

    public override string NextButtonText
    {
        get
        {
            return CurrentStep switch
            {
                3 => "Generuj", // Step 3: Processing step
                _ => "Další"
            };
        }
    }

    protected override void InitializeSteps()
    {
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte složku s šablonami", IsActive = true },  // Step 0: Existing Folder Selection
            new() { Title = "Nalezené zástupné symboly" },                    // Step 1: Placeholder Discovery  
            new() { Title = "Zadání hodnot zástupných symbolů" },             // Step 2: Placeholder Input  
            new() { Title = "Zpracování a výsledky" }                         // Step 3: Processing Results
        };
        Steps = new ReadOnlyCollection<StepInfo>(stepList);
    }

    protected override void InitializeViewModels()
    {
        // Step 0: Existing Folder Selection
        _existingFolderSelectionViewModel = ServiceProvider.GetRequiredService<ExistingProjectFolderSelectionViewModel>();

        // Step 1: Placeholder Discovery
        _placeholderDiscoveryViewModel = ServiceProvider.GetRequiredService<PlaceholderDiscoveryViewModel>();

        // Step 2: Placeholder Input
        _placeholderInputViewModel = ServiceProvider.GetRequiredService<PlaceholderInputViewModel>();

        // Step 3: Processing Results
        _processingResultsViewModel = ServiceProvider.GetRequiredService<ProcessingResultsViewModel>();
        _processingResultsViewModel.RequestNavigationToStep += (step) => {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                CurrentStep = Math.Max(0, Math.Min(step, TotalSteps - 1));
                UpdateStepStates(CurrentStep);
            });
        };

        StepViewModels = new List<StepViewModelBase>
        {
            _existingFolderSelectionViewModel, // Step 0
            _placeholderDiscoveryViewModel,    // Step 1
            _placeholderInputViewModel,        // Step 2
            _processingResultsViewModel        // Step 3
        };
    }

    protected override void InitializeViews()
    {
        var step0View = new Step1ExistingProjectFolderSelectionView
        {
            DataContext = _existingFolderSelectionViewModel
        };

        var step1View = new Step2PlaceholderDiscoveryView
        {
            DataContext = _placeholderDiscoveryViewModel
        };

        var step2View = new Step3PlaceholderInputView
        {
            DataContext = _placeholderInputViewModel
        };

        var step3View = new Step5ProcessingResultsView
        {
            DataContext = _processingResultsViewModel
        };

        StepViews = new List<UserControl>
        {
            step0View,  // Step 0: Existing Folder Selection
            step1View,  // Step 1: Placeholder Discovery
            step2View,  // Step 2: Placeholder Input
            step3View   // Step 3: Processing Results
        };
    }

    protected override void PrepareStepTransition(int fromStep, int toStep)
    {
        switch (toStep)
        {
            case 1: // Moving to Placeholder Discovery
                if (!string.IsNullOrEmpty(_existingFolderSelectionViewModel.SelectedFolderPath))
                {
                    // Set the selected folder as the template set for scanning
                    var templateSetInfo = new TemplateSetInfo
                    {
                        Name = System.IO.Path.GetFileName(_existingFolderSelectionViewModel.SelectedFolderPath) ?? "Existing Project",
                        Path = _existingFolderSelectionViewModel.SelectedFolderPath,
                        FileCount = _existingFolderSelectionViewModel.DocxFileCount,
                        TotalSize = 0, // Not relevant for update mode
                        TotalSizeFormatted = "N/A",
                        LastModified = DateTime.Now
                    };
                    
                    var templateSetItem = new TemplateSetItemViewModel(templateSetInfo)
                    {
                        IsSelected = true
                    };

                    _placeholderDiscoveryViewModel.SetSelectedTemplateSet(templateSetItem);
                    
                    // Trigger placeholder scanning
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                        await _placeholderDiscoveryViewModel.ScanPlaceholdersAsync();
                    });
                }
                break;

            case 2: // Moving to Placeholder Input
                var discoveredPlaceholders = _placeholderDiscoveryViewModel.DiscoveredPlaceholders;
                
                // Filter out SOUBOR_PREFIX for update workflow as per requirements
                var filteredPlaceholders = discoveredPlaceholders
                    .Where(p => p.Name != "SOUBOR_PREFIX")
                    .ToList();
                    
                _placeholderInputViewModel.SetDiscoveredPlaceholders(filteredPlaceholders);
                break;

            case 3: // Moving to Processing
                if (!string.IsNullOrEmpty(_existingFolderSelectionViewModel.SelectedFolderPath))
                {
                    // For update mode, source and target are the same (in-place processing)
                    _processingResultsViewModel.SetProcessingData(
                        _existingFolderSelectionViewModel.SelectedFolderPath, // Source folder (existing files)
                        _existingFolderSelectionViewModel.SelectedFolderPath, // Target folder (same as source)
                        _placeholderInputViewModel.GetReplacementMapping(),
                        ProcessingMode.UpdateProject);
                        
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
        // Update project wizard completion logic
        // Could show summary or navigate to results
    }

    /// <summary>
    /// Get the selected existing project folder path (for external access)
    /// </summary>
    public string? SelectedFolderPath => _existingFolderSelectionViewModel.SelectedFolderPath;

    /// <summary>
    /// Get the discovered placeholders (for external access)
    /// </summary>
    public IEnumerable<PlaceholderItemViewModel> DiscoveredPlaceholders => _placeholderDiscoveryViewModel.DiscoveredPlaceholders;

    /// <summary>
    /// Get the placeholder replacement mappings (for external access)
    /// </summary>
    public Dictionary<string, string> PlaceholderMappings => _placeholderInputViewModel.GetReplacementMapping();

    /// <summary>
    /// Get the number of DOCX files found in the selected folder
    /// </summary>
    public int DocxFileCount => _existingFolderSelectionViewModel.DocxFileCount;
}