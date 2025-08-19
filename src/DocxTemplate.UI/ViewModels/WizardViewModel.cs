using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Views.Steps;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DocxTemplate.UI.ViewModels;

public class WizardViewModel : ViewModelBase
{
    private int _currentStep = 1;
    private const int TotalSteps = 4;
    private readonly List<UserControl> _stepViews;
    private readonly List<StepViewModelBase> _stepViewModels;
    private readonly IServiceProvider _serviceProvider;
    private IDisposable? _validationSubscription;

    public WizardViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte sadu šablon", IsActive = true },
            new() { Title = "Zadání hodnot zástupných symbolů" },
            new() { Title = "Výběr výstupní složky" },
            new() { Title = "Zpracování a výsledky" }
        };
        Steps = new ReadOnlyCollection<StepInfo>(stepList);

        // Initialize step ViewModels
        var processingResultsViewModel = _serviceProvider.GetRequiredService<ProcessingResultsViewModel>();
        processingResultsViewModel.RequestNavigationToStep += (step) => CurrentStep = step;
        
        _stepViewModels = new List<StepViewModelBase>
        {
            _serviceProvider.GetRequiredService<TemplateSetSelectionViewModel>(),
            _serviceProvider.GetRequiredService<PlaceholderInputViewModel>(),
            _serviceProvider.GetRequiredService<OutputFolderSelectionViewModel>(),
            processingResultsViewModel
        };

        // Initialize step views with their ViewModels
        var step1View = new Step1TemplateSelectionView
        {
            DataContext = _stepViewModels[0]
        };
        
        var step2View = new Step3PlaceholderInputView
        {
            DataContext = _stepViewModels[1]
        };
        
        var step3View = new Step4OutputSelectionView
        {
            DataContext = _stepViewModels[2]
        };
        
        var step4View = new Step5ProcessingResultsView
        {
            DataContext = _stepViewModels[3]
        };
        
        _stepViews = new List<UserControl>
        {
            step1View,
            step2View,
            step3View,
            step4View
        };

        var canGoBack = this.WhenAnyValue(x => x.CurrentStep)
            .Select(step => step > 1 && step < TotalSteps)  // Hide back button on final step
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
        
        // Activate the first step
        _stepViewModels[0]?.OnStepActivated();
    }

    public ReadOnlyCollection<StepInfo> Steps { get; }

    public int CurrentStep
    {
        get => _currentStep;
        set => this.RaiseAndSetIfChanged(ref _currentStep, value);
    }

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

    public string CurrentStepTitle => Steps[CurrentStep - 1].Title;

    public string StepIndicatorText => $"Krok {CurrentStep} z {TotalSteps}";

    public UserControl CurrentStepContent => _stepViews[CurrentStep - 1];

    public ReactiveCommand<Unit, Unit> NextCommand { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }
    
    /// <summary>
    /// Indicates whether the back button should be visible
    /// </summary>
    public bool IsBackButtonVisible => CurrentStep > 1 && CurrentStep < TotalSteps;
    
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
            var currentStepViewModel = _stepViewModels[CurrentStep - 1];
            currentStepViewModel?.OnStepDeactivated();
            
            // Pass data between steps if needed
            await TransferDataBetweenStepsAsync(CurrentStep, CurrentStep + 1);
            
            Steps[CurrentStep - 1].IsCompleted = true;
            CurrentStep++;
            
            // Activate next step
            var nextStepViewModel = _stepViewModels[CurrentStep - 1];
            nextStepViewModel?.OnStepActivated();
        }
    }

    private void GoToPreviousStep()
    {
        if (CurrentStep > 1)
        {
            // Deactivate current step
            var currentStepViewModel = _stepViewModels[CurrentStep - 1];
            currentStepViewModel?.OnStepDeactivated();
            
            CurrentStep--;
            
            // Activate previous step
            var previousStepViewModel = _stepViewModels[CurrentStep - 1];
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
            Steps[i].IsActive = (i + 1) == currentStep;
        }

        // Dispose previous subscription
        _validationSubscription?.Dispose();
        
        // Update CanAdvanceToNextStep based on current step validation
        var currentStepViewModel = _stepViewModels[currentStep - 1];
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
    /// <param name="fromStep">Source step number (1-based)</param>
    /// <param name="toStep">Target step number (1-based)</param>
    private async Task TransferDataBetweenStepsAsync(int fromStep, int toStep)
    {
        // Step 1 to Step 2: Automatically discover placeholders and pass to input step
        if (fromStep == 1 && toStep == 2)
        {
            var step1ViewModel = _stepViewModels[0] as TemplateSetSelectionViewModel;
            var step2ViewModel = _stepViewModels[1] as PlaceholderInputViewModel;
            
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
        
        // Step 3 to Step 4: Pass all processing data for final step
        if (fromStep == 3 && toStep == 4)
        {
            var step1ViewModel = _stepViewModels[0] as TemplateSetSelectionViewModel;
            var step2ViewModel = _stepViewModels[1] as PlaceholderInputViewModel;
            var step3ViewModel = _stepViewModels[2] as OutputFolderSelectionViewModel;
            var step4ViewModel = _stepViewModels[3] as ProcessingResultsViewModel;
            
            if (step1ViewModel?.SelectedTemplateSet != null && 
                step2ViewModel != null && 
                step3ViewModel?.SelectedFolderPath != null && 
                step4ViewModel != null)
            {
                step4ViewModel.SetProcessingData(
                    step1ViewModel.SelectedTemplateSet.TemplateSetInfo.Path,
                    step3ViewModel.SelectedFolderPath,
                    step2ViewModel.GetReplacementMapping());
            }
        }
    }
}