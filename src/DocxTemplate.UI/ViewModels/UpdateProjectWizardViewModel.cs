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
using ReactiveUI;

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

    public override int TotalSteps => 3;

    public override string NextButtonText
    {
        get
        {
            return CurrentStep switch
            {
                2 when _processingResultsViewModel.IsProcessingComplete => "Konec", // Step 2: After processing is complete
                2 => "Generuj", // Step 2: Before processing starts
                _ => "Další"
            };
        }
    }

    protected override void InitializeSteps()
    {
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte složku s šablonami", IsActive = true },  // Step 0: Existing Folder Selection
            new() { Title = "Zadání hodnot zástupných symbolů" },             // Step 1: Placeholder Input (skip discovery)
            new() { Title = "Zpracování a výsledky" }                         // Step 2: Processing Results
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
        
        // Subscribe to processing completion to update button text
        _processingResultsViewModel.WhenAnyValue(x => x.IsProcessingComplete)
            .Subscribe(_ => {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    this.RaisePropertyChanged(nameof(NextButtonText));
                });
            });

        StepViewModels = new List<StepViewModelBase>
        {
            _existingFolderSelectionViewModel, // Step 0: Existing Folder Selection
            _placeholderInputViewModel,        // Step 1: Placeholder Input (skip discovery)
            _processingResultsViewModel        // Step 2: Processing Results
        };
    }

    protected override void InitializeViews()
    {
        var step0View = new Step1ExistingProjectFolderSelectionView
        {
            DataContext = _existingFolderSelectionViewModel
        };

        var step1View = new Step3PlaceholderInputView
        {
            DataContext = _placeholderInputViewModel
        };

        var step2View = new Step5ProcessingResultsView
        {
            DataContext = _processingResultsViewModel
        };

        StepViews = new List<UserControl>
        {
            step0View,  // Step 0: Existing Folder Selection
            step1View,  // Step 1: Placeholder Input (skip discovery)
            step2View   // Step 2: Processing Results
        };
    }

    protected override void PrepareStepTransition(int fromStep, int toStep)
    {
        switch (toStep)
        {
            case 1: // Moving to Placeholder Input (skip discovery)
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
                    
                    // Trigger placeholder scanning and then transfer to input step
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                        await _placeholderDiscoveryViewModel.ScanPlaceholdersAsync();
                        
                        var discoveredPlaceholders = _placeholderDiscoveryViewModel.DiscoveredPlaceholders;
                        
                        // Filter out SOUBOR_PREFIX for update workflow as per requirements
                        var filteredPlaceholders = discoveredPlaceholders
                            .Where(p => p.Name != "SOUBOR_PREFIX")
                            .ToList();
                            
                        _placeholderInputViewModel.SetDiscoveredPlaceholders(filteredPlaceholders);
                    });
                }
                break;

            case 2: // Moving to Processing (now step 2 instead of 3)
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

    protected override void GoToNextStep()
    {
        // If button shows "Konec" (End), close the application
        if (NextButtonText == "Konec")
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                System.Environment.Exit(0);
            });
            return;
        }
        
        // Otherwise, proceed with normal step navigation
        base.GoToNextStep();
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