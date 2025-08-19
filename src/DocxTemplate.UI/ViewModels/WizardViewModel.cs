using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Views.Steps;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DocxTemplate.UI.ViewModels;

public class WizardViewModel : ViewModelBase
{
    private int _currentStep = 0;  // Start at mode selection (Step 0)
    private ProcessingMode _selectedMode = ProcessingMode.NewProject;
    private List<UserControl> _stepViews = new();
    private List<StepViewModelBase> _stepViewModels = new();
    private readonly IServiceProvider _serviceProvider;
    private IDisposable? _validationSubscription;

    public WizardViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // Create steps for Mode 1 (NewProject) - initial default
        CreateStepsForNewProject();
        
        // Initialize steps for current mode
        UpdateStepsForMode(SelectedMode);

        // Initialize step ViewModels
        InitializeViewModels();

        // Initialize step views
        InitializeViews();

        var canGoBack = this.WhenAnyValue(x => x.CurrentStep)
            .Select(step => step > 0 && step < TotalSteps)  // Hide back button on final step, allow back from step 1
            .ObserveOn(RxApp.MainThreadScheduler);
            
        var canGoNext = this.WhenAnyValue(x => x.CurrentStep)
            .ObserveOn(RxApp.MainThreadScheduler)
            .CombineLatest(
                this.WhenAnyValue(x => x.CanAdvanceToNextStep).ObserveOn(RxApp.MainThreadScheduler), 
                (step, canAdvance) => step < TotalSteps && canAdvance)
            .ObserveOn(RxApp.MainThreadScheduler);
            
        var canFinish = this.WhenAnyValue(x => x.CurrentStep)
            .Select(step => step == TotalSteps)
            .ObserveOn(RxApp.MainThreadScheduler);

        NextCommand = ReactiveCommand.Create(GoToNextStep, canGoNext, RxApp.MainThreadScheduler);
        BackCommand = ReactiveCommand.Create(GoToPreviousStep, canGoBack, RxApp.MainThreadScheduler);
        FinishCommand = ReactiveCommand.Create(FinishWizard, canFinish, RxApp.MainThreadScheduler);

        this.WhenAnyValue(x => x.CurrentStep)
            .Subscribe(UpdateStepStates);
        
        // Activate the first step (Step 0: Mode Selection)
        _stepViewModels[0]?.OnStepActivated();
    }

    public ReadOnlyCollection<StepInfo> Steps { get; private set; } = null!;

    public int CurrentStep
    {
        get => _currentStep;
        set => this.RaiseAndSetIfChanged(ref _currentStep, value);
    }

    /// <summary>
    /// The selected processing mode that determines the workflow
    /// </summary>
    public ProcessingMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            var previousValue = _selectedMode;
            this.RaiseAndSetIfChanged(ref _selectedMode, value);
            if (previousValue != _selectedMode)  // Only swap if value actually changed
            {
                SwapWorkflowForMode(value);
                this.RaisePropertyChanged(nameof(TotalSteps));
                this.RaisePropertyChanged(nameof(StepIndicatorText));
            }
        }
    }

    /// <summary>
    /// Total steps in wizard - varies based on selected mode
    /// NewProject: 5 steps (0: Mode Selection + 1: Template Selection + 2: Placeholder Input + 3: Output Folder + 4: Processing)
    /// UpdateProject: 5 steps (0: Mode Selection + 1: Existing Folder Selection + 2: Placeholder Input + 3: Processing + 4: Results)
    /// Both modes have 5 steps total, but different workflows
    /// </summary>
    public int TotalSteps => 5;  // Both workflows have 5 steps (0-4)

    private bool _canAdvanceToNextStep = true;
    public bool CanAdvanceToNextStep
    {
        get => _canAdvanceToNextStep;
        set
        {
            // Ensure property changes happen on UI thread
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                this.RaiseAndSetIfChanged(ref _canAdvanceToNextStep, value);
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                    this.RaiseAndSetIfChanged(ref _canAdvanceToNextStep, value));
            }
        }
    }

    public string CurrentStepTitle => Steps[CurrentStep].Title;

    public string StepIndicatorText => $"Krok {CurrentStep} z {TotalSteps - 1}";  // Display as 0-4 instead of 1-5

    public UserControl CurrentStepContent => _stepViews[CurrentStep];

    /// <summary>
    /// Dynamic button text based on current step and mode
    /// </summary>
    public string NextButtonText
    {
        get
        {
            if (SelectedMode == ProcessingMode.NewProject)
            {
                // NewProject: Step 4 is processing/results, show "Generuj" at step 3 to advance to processing
                return CurrentStep == 3 ? "Generuj" : "Další";
            }
            else // UpdateProject
            {
                // UpdateProject: Step 3 is processing, show "Generuj" at step 2 to advance to processing
                return CurrentStep == 2 ? "Generuj" : "Další";
            }
        }
    }

    public ReactiveCommand<Unit, Unit> NextCommand { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }
    
    /// <summary>
    /// Indicates whether the back button should be visible
    /// </summary>
    public bool IsBackButtonVisible => CurrentStep > 0 && CurrentStep < TotalSteps;
    
    /// <summary>
    /// Indicates whether the next button should be visible
    /// </summary>
    public bool IsNextButtonVisible => CurrentStep < TotalSteps;
    
    /// <summary>
    /// Indicates whether the finish button should be visible
    /// </summary>
    public bool IsFinishButtonVisible => CurrentStep == TotalSteps;

    private async void GoToNextStep()
    {
        if (CurrentStep < TotalSteps)
        {
            // Deactivate current step
            var currentStepViewModel = _stepViewModels[CurrentStep];
            currentStepViewModel?.OnStepDeactivated();
            
            // Pass data between steps if needed
            await TransferDataBetweenStepsAsync(CurrentStep, CurrentStep + 1);
            
            Steps[CurrentStep].IsCompleted = true;
            CurrentStep++;
            
            // Activate next step
            var nextStepViewModel = _stepViewModels[CurrentStep];
            nextStepViewModel?.OnStepActivated();
            
            // Auto-start processing when entering the final step (step 4)
            if (CurrentStep == 4)
            {
                var processingResultsViewModel = nextStepViewModel as ProcessingResultsViewModel;
                if (processingResultsViewModel != null)
                {
                    // Start processing automatically
                    _ = Task.Run(async () =>
                    {
                        // Small delay to let the UI update
                        await Task.Delay(100);
                        
                        // Execute the processing command on UI thread
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var canExecute = await processingResultsViewModel.ProcessTemplatesCommand.CanExecute.FirstAsync();
                            if (canExecute)
                            {
                                processingResultsViewModel.ProcessTemplatesCommand.Execute().Subscribe();
                            }
                        });
                    });
                }
            }
        }
    }

    private void GoToPreviousStep()
    {
        if (CurrentStep > 0)
        {
            // Deactivate current step
            var currentStepViewModel = _stepViewModels[CurrentStep];
            currentStepViewModel?.OnStepDeactivated();
            
            CurrentStep--;
            
            // Activate previous step
            var previousStepViewModel = _stepViewModels[CurrentStep];
            previousStepViewModel?.OnStepActivated();
        }
    }

    private void UpdateStepStates(int currentStep)
    {
        // Ensure we're on UI thread
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateStepStates(currentStep));
            return;
        }
        
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsActive = i == currentStep;
        }

        // Dispose previous subscription
        _validationSubscription?.Dispose();
        
        // Update CanAdvanceToNextStep based on current step validation
        var currentStepViewModel = _stepViewModels[currentStep];
        CanAdvanceToNextStep = currentStepViewModel?.ValidateStep() ?? true;
        
        // Subscribe to validation changes for reactive updates
        if (currentStepViewModel != null)
        {
            _validationSubscription = currentStepViewModel.WhenAnyValue(x => x.IsValid)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isValid => CanAdvanceToNextStep = isValid);
        }

        this.RaisePropertyChanged(nameof(CurrentStepTitle));
        this.RaisePropertyChanged(nameof(StepIndicatorText));
        this.RaisePropertyChanged(nameof(CurrentStepContent));
        this.RaisePropertyChanged(nameof(NextButtonText));
        this.RaisePropertyChanged(nameof(IsBackButtonVisible));
        this.RaisePropertyChanged(nameof(IsNextButtonVisible));
        this.RaisePropertyChanged(nameof(IsFinishButtonVisible));
    }
    
    /// <summary>
    /// Finishes the wizard and closes the application
    /// </summary>
    private void FinishWizard()
    {
        // Close the application
        Environment.Exit(0);
    }

    /// <summary>
    /// Transfers data between wizard steps as user navigates
    /// </summary>
    /// <param name="fromStep">Source step number (0-based)</param>
    /// <param name="toStep">Target step number (0-based)</param>
    private async Task TransferDataBetweenStepsAsync(int fromStep, int toStep)
    {
        // Step 1 to Step 2: Automatically discover placeholders and pass to input step
        if (fromStep == 1 && toStep == 2)
        {
            if (SelectedMode == ProcessingMode.NewProject)
            {
                // Mode 1: Template set selection to placeholder input
                var step1ViewModel = _stepViewModels[1] as TemplateSetSelectionViewModel;
                var step2ViewModel = _stepViewModels[2] as PlaceholderInputViewModel;
                
                if (step1ViewModel?.SelectedTemplateSet != null && step2ViewModel != null)
                {
                    // Create a hidden placeholder discovery to get the placeholders
                    var placeholderDiscoveryViewModel = _serviceProvider.GetRequiredService<PlaceholderDiscoveryViewModel>();
                    placeholderDiscoveryViewModel.SetSelectedTemplateSet(step1ViewModel.SelectedTemplateSet);
                    
                    // Trigger placeholder scanning and wait for completion
                    await placeholderDiscoveryViewModel.ScanPlaceholdersAsync();
                    
                    // Pass discovered placeholders to input step
                    step2ViewModel.SetDiscoveredPlaceholders(placeholderDiscoveryViewModel.DiscoveredPlaceholders);
                }
            }
            else if (SelectedMode == ProcessingMode.UpdateProject)
            {
                // Mode 2: Existing folder selection to placeholder input
                var step1ViewModel = _stepViewModels[1] as ExistingProjectFolderSelectionViewModel;
                var step2ViewModel = _stepViewModels[2] as PlaceholderInputViewModel;
                
                if (step1ViewModel?.SelectedFolderPath != null && step2ViewModel != null)
                {
                    // Use IPlaceholderScanService to scan all .docx files in the folder (recursively)
                    var placeholderScanService = _serviceProvider.GetRequiredService<DocxTemplate.Core.Services.IPlaceholderScanService>();
                    
                    try
                    {
                        // Scan all .docx files recursively in the selected folder
                        var scanResult = await placeholderScanService.ScanPlaceholdersAsync(step1ViewModel.SelectedFolderPath, recursive: true);
                        
                        // Convert scan result to PlaceholderItemViewModel objects for the input step
                        var discoveredPlaceholders = new List<PlaceholderItemViewModel>();
                        foreach (var placeholder in scanResult.Placeholders)
                        {
                            // Filter out {{SOUBOR_PREFIX}} placeholder as specified in requirements
                            if (placeholder.Name != "SOUBOR_PREFIX")
                            {
                                discoveredPlaceholders.Add(new PlaceholderItemViewModel(placeholder));
                            }
                        }
                        
                        // Pass discovered placeholders to input step
                        step2ViewModel.SetDiscoveredPlaceholders(discoveredPlaceholders);
                    }
                    catch (Exception)
                    {
                        // Handle scanning errors gracefully - create empty placeholder list
                        // Error will be shown in the UI through validation
                        step2ViewModel.SetDiscoveredPlaceholders(new List<PlaceholderItemViewModel>());
                    }
                }
            }
        }
        
        // Step 3 to Step 4: Pass all processing data for final step
        if (fromStep == 3 && toStep == 4)
        {
            if (SelectedMode == ProcessingMode.NewProject)
            {
                // Mode 1: Standard workflow with template set, placeholder input, and output folder
                var step1ViewModel = _stepViewModels[1] as TemplateSetSelectionViewModel;
                var step2ViewModel = _stepViewModels[2] as PlaceholderInputViewModel;
                var step3ViewModel = _stepViewModels[3] as OutputFolderSelectionViewModel;
                var step4ViewModel = _stepViewModels[4] as ProcessingResultsViewModel;
                
                if (step1ViewModel?.SelectedTemplateSet != null && 
                    step2ViewModel != null && 
                    step3ViewModel?.SelectedFolderPath != null && 
                    step4ViewModel != null)
                {
                    step4ViewModel.SetProcessingData(
                        step1ViewModel.SelectedTemplateSet.TemplateSetInfo.Path,
                        step3ViewModel.SelectedFolderPath,
                        step2ViewModel.GetReplacementMapping(),
                        ProcessingMode.NewProject);
                }
            }
            else if (SelectedMode == ProcessingMode.UpdateProject)
            {
                // Mode 2: Update existing files workflow - process in-place
                var step1ViewModel = _stepViewModels[1] as ExistingProjectFolderSelectionViewModel;
                var step2ViewModel = _stepViewModels[2] as PlaceholderInputViewModel;
                var step4ViewModel = _stepViewModels[3] as ProcessingResultsViewModel; // Step 3 in Mode 2 is processing
                
                if (step1ViewModel?.SelectedFolderPath != null && 
                    step2ViewModel != null && 
                    step4ViewModel != null)
                {
                    // For Mode 2, we process files in-place, so source and target are the same
                    step4ViewModel.SetProcessingData(
                        step1ViewModel.SelectedFolderPath, // Source folder (contains existing files)
                        step1ViewModel.SelectedFolderPath, // Target folder (same as source for in-place updates)
                        step2ViewModel.GetReplacementMapping(),
                        ProcessingMode.UpdateProject);
                }
            }
        }
    }

    /// <summary>
    /// Initialize ViewModels for all steps
    /// </summary>
    private void InitializeViewModels()
    {
        // Step 0: Mode Selection
        var modeSelectionViewModel = _serviceProvider.GetRequiredService<ModeSelectionViewModel>();
        modeSelectionViewModel.ModeSelected += (mode) => {
            SelectedMode = mode;
            // Auto-advance when mode is selected
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (CurrentStep == 0)
                {
                    GoToNextStep();
                }
            });
        };

        // Step 1: Template Selection (Mode 1) OR Existing Project Folder Selection (Mode 2)
        var templateSelectionViewModel = _serviceProvider.GetRequiredService<TemplateSetSelectionViewModel>();
        templateSelectionViewModel.TemplateSelected += () => {
            // Auto-advance to placeholder step when template is selected (Mode 1 only)
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (CurrentStep == 1 && SelectedMode == ProcessingMode.NewProject && templateSelectionViewModel.SelectedTemplateSet != null)
                {
                    GoToNextStep();
                }
            });
        };

        var existingProjectFolderViewModel = _serviceProvider.GetRequiredService<ExistingProjectFolderSelectionViewModel>();

        // Step 2: Placeholder Input (both modes)
        var placeholderInputViewModel = _serviceProvider.GetRequiredService<PlaceholderInputViewModel>();

        // Step 3: Output Folder Selection (Mode 1) OR Processing (Mode 2)  
        var outputFolderSelectionViewModel = _serviceProvider.GetRequiredService<OutputFolderSelectionViewModel>();

        // Step 4: Processing Results (both modes)
        var processingResultsViewModel = _serviceProvider.GetRequiredService<ProcessingResultsViewModel>();
        processingResultsViewModel.RequestNavigationToStep += (step) => CurrentStep = step;

        _stepViewModels = new List<StepViewModelBase>
        {
            modeSelectionViewModel,                 // Step 0: Mode Selection
            templateSelectionViewModel,            // Step 1: Template Selection (will be swapped for Mode 2)
            placeholderInputViewModel,             // Step 2: Placeholder Input
            outputFolderSelectionViewModel,        // Step 3: Output Folder Selection (will be swapped for Mode 2)
            processingResultsViewModel             // Step 4: Processing Results
        };

        // Store references for mode-specific swapping
        _templateSelectionViewModel = templateSelectionViewModel;
        _existingProjectFolderViewModel = existingProjectFolderViewModel;
        _outputFolderSelectionViewModel = outputFolderSelectionViewModel;
    }

    /// <summary>
    /// Initialize Views for all steps
    /// </summary>
    private void InitializeViews()
    {
        var step0View = new Step0ModeSelectionView
        {
            DataContext = _stepViewModels[0]
        };

        var step1TemplateView = new Step1TemplateSelectionView
        {
            DataContext = _stepViewModels[1]
        };

        var step1ExistingProjectView = new Step1ExistingProjectFolderSelectionView
        {
            DataContext = _existingProjectFolderViewModel
        };

        var step2View = new Step3PlaceholderInputView
        {
            DataContext = _stepViewModels[2]
        };

        var step3OutputView = new Step4OutputSelectionView
        {
            DataContext = _stepViewModels[3]
        };

        // For Mode 2, we'll need a processing view instead of output selection
        // For now, use the results view as processing
        var step3ProcessingView = new Step5ProcessingResultsView
        {
            DataContext = _stepViewModels[4]
        };

        var step4View = new Step5ProcessingResultsView
        {
            DataContext = _stepViewModels[4]
        };

        _stepViews = new List<UserControl>
        {
            step0View,              // Step 0: Mode Selection
            step1TemplateView,      // Step 1: Template Selection (will be swapped for Mode 2)
            step2View,              // Step 2: Placeholder Input
            step3OutputView,        // Step 3: Output Folder Selection (will be swapped for Mode 2)
            step4View               // Step 4: Processing Results
        };

        // Store references for mode-specific swapping
        _step1TemplateView = step1TemplateView;
        _step1ExistingProjectView = step1ExistingProjectView;
        _step3OutputView = step3OutputView;
        _step3ProcessingView = step3ProcessingView;
    }

    /// <summary>
    /// Create steps for Mode 1 (NewProject)
    /// </summary>
    private void CreateStepsForNewProject()
    {
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte režim zpracování", IsActive = true },  // Step 0: Mode Selection
            new() { Title = "Vyberte sadu šablon" },                        // Step 1: Template Selection
            new() { Title = "Zadání hodnot zástupných symbolů" },           // Step 2: Placeholder Input  
            new() { Title = "Výběr výstupní složky" },                      // Step 3: Output Folder Selection
            new() { Title = "Zpracování a výsledky" }                       // Step 4: Processing Results
        };
        Steps = new ReadOnlyCollection<StepInfo>(stepList);
    }

    /// <summary>
    /// Create steps for Mode 2 (UpdateProject)
    /// </summary>
    private void CreateStepsForUpdateProject()
    {
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte režim zpracování", IsActive = true },  // Step 0: Mode Selection
            new() { Title = "Vyberte složku s šablonami" },                 // Step 1: Existing Folder Selection
            new() { Title = "Zadání hodnot zástupných symbolů" },           // Step 2: Placeholder Input  
            new() { Title = "Zpracování šablon" },                          // Step 3: Processing
            new() { Title = "Výsledky" }                                    // Step 4: Results
        };
        Steps = new ReadOnlyCollection<StepInfo>(stepList);
    }

    /// <summary>
    /// Update steps based on selected processing mode
    /// </summary>
    private void UpdateStepsForMode(ProcessingMode mode)
    {
        if (mode == ProcessingMode.NewProject)
        {
            CreateStepsForNewProject();
        }
        else if (mode == ProcessingMode.UpdateProject)
        {
            CreateStepsForUpdateProject();
        }
    }

    /// <summary>
    /// Swap ViewModels and Views based on selected processing mode
    /// </summary>
    private void SwapWorkflowForMode(ProcessingMode mode)
    {
        // Update steps first
        UpdateStepsForMode(mode);
        
        if (mode == ProcessingMode.NewProject)
        {
            // Mode 1: Template Selection workflow
            _stepViewModels[1] = _templateSelectionViewModel;
            _stepViewModels[3] = _outputFolderSelectionViewModel;
            
            _stepViews[1] = _step1TemplateView;
            _stepViews[3] = _step3OutputView;
        }
        else if (mode == ProcessingMode.UpdateProject)
        {
            // Mode 2: Existing project folder workflow
            _stepViewModels[1] = _existingProjectFolderViewModel;
            _stepViewModels[3] = _stepViewModels[4]; // Use processing results for step 3 in Mode 2
            
            _stepViews[1] = _step1ExistingProjectView;
            _stepViews[3] = _step3ProcessingView;
        }
        
        // Force UI updates
        this.RaisePropertyChanged(nameof(Steps));
        this.RaisePropertyChanged(nameof(CurrentStepContent));
        this.RaisePropertyChanged(nameof(CurrentStepTitle));
    }

    // Fields to store ViewModels and Views for swapping between modes
    private TemplateSetSelectionViewModel _templateSelectionViewModel = null!;
    private ExistingProjectFolderSelectionViewModel _existingProjectFolderViewModel = null!;
    private OutputFolderSelectionViewModel _outputFolderSelectionViewModel = null!;
    
    private UserControl _step1TemplateView = null!;
    private UserControl _step1ExistingProjectView = null!;
    private UserControl _step3OutputView = null!;
    private UserControl _step3ProcessingView = null!;
}