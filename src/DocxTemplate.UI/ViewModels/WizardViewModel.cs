using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
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
    private const int TotalSteps = 5;
    private readonly List<UserControl> _stepViews;
    private readonly List<StepViewModelBase> _stepViewModels;
    private readonly IServiceProvider _serviceProvider;

    public WizardViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte sadu šablon", IsActive = true },
            new() { Title = "Nalezení zástupných symbolů" },
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
            _serviceProvider.GetRequiredService<PlaceholderDiscoveryViewModel>(),
            _serviceProvider.GetRequiredService<PlaceholderInputViewModel>(),
            _serviceProvider.GetRequiredService<OutputFolderSelectionViewModel>(),
            processingResultsViewModel
        };

        // Initialize step views with their ViewModels
        var step1View = new Step1TemplateSelectionView
        {
            DataContext = _stepViewModels[0]
        };
        
        var step2View = new Step2PlaceholderDiscoveryView
        {
            DataContext = _stepViewModels[1]
        };
        
        var step3View = new Step3PlaceholderInputView
        {
            DataContext = _stepViewModels[2]
        };
        
        var step4View = new Step4OutputSelectionView
        {
            DataContext = _stepViewModels[3]
        };
        
        var step5View = new Step5ProcessingResultsView
        {
            DataContext = _stepViewModels[4]
        };
        
        _stepViews = new List<UserControl>
        {
            step1View,
            step2View,
            step3View,
            step4View,
            step5View
        };

        var canGoBack = this.WhenAnyValue(x => x.CurrentStep, step => step > 1);
        var canGoNext = this.WhenAnyValue(x => x.CurrentStep, step => step < TotalSteps)
            .CombineLatest(this.WhenAnyValue(x => x.CanAdvanceToNextStep), (hasNext, canAdvance) => hasNext && canAdvance);

        NextCommand = ReactiveCommand.Create(GoToNextStep, canGoNext);
        BackCommand = ReactiveCommand.Create(GoToPreviousStep, canGoBack);

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
        set => this.RaiseAndSetIfChanged(ref _canAdvanceToNextStep, value);
    }

    public string CurrentStepTitle => Steps[CurrentStep - 1].Title;

    public string StepIndicatorText => $"Krok {CurrentStep} z {TotalSteps}";

    public UserControl CurrentStepContent => _stepViews[CurrentStep - 1];

    public ReactiveCommand<Unit, Unit> NextCommand { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    private void GoToNextStep()
    {
        if (CurrentStep < TotalSteps)
        {
            // Deactivate current step
            var currentStepViewModel = _stepViewModels[CurrentStep - 1];
            currentStepViewModel?.OnStepDeactivated();
            
            // Pass data between steps if needed
            TransferDataBetweenSteps(CurrentStep, CurrentStep + 1);
            
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
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsActive = (i + 1) == currentStep;
        }

        // Update CanAdvanceToNextStep based on current step validation
        var currentStepViewModel = _stepViewModels[currentStep - 1];
        CanAdvanceToNextStep = currentStepViewModel?.ValidateStep() ?? true;
        
        // Subscribe to validation changes for reactive updates
        if (currentStepViewModel != null)
        {
            currentStepViewModel.WhenAnyValue(x => x.IsValid)
                .Subscribe(isValid => CanAdvanceToNextStep = isValid);
        }

        this.RaisePropertyChanged(nameof(CurrentStepTitle));
        this.RaisePropertyChanged(nameof(StepIndicatorText));
        this.RaisePropertyChanged(nameof(CurrentStepContent));
    }

    /// <summary>
    /// Transfers data between wizard steps as user navigates
    /// </summary>
    /// <param name="fromStep">Source step number (1-based)</param>
    /// <param name="toStep">Target step number (1-based)</param>
    private void TransferDataBetweenSteps(int fromStep, int toStep)
    {
        // Step 1 to Step 2: Pass selected template set for placeholder scanning
        if (fromStep == 1 && toStep == 2)
        {
            var step1ViewModel = _stepViewModels[0] as TemplateSetSelectionViewModel;
            var step2ViewModel = _stepViewModels[1] as PlaceholderDiscoveryViewModel;
            
            if (step1ViewModel?.SelectedTemplateSet != null && step2ViewModel != null)
            {
                step2ViewModel.SetSelectedTemplateSet(step1ViewModel.SelectedTemplateSet);
            }
        }
        
        // Step 2 to Step 3: Pass discovered placeholders for input
        if (fromStep == 2 && toStep == 3)
        {
            var step2ViewModel = _stepViewModels[1] as PlaceholderDiscoveryViewModel;
            var step3ViewModel = _stepViewModels[2] as PlaceholderInputViewModel;
            
            if (step2ViewModel != null && step3ViewModel != null)
            {
                step3ViewModel.SetDiscoveredPlaceholders(step2ViewModel.DiscoveredPlaceholders);
            }
        }
        
        // Step 4 to Step 5: Pass all processing data for final step
        if (fromStep == 4 && toStep == 5)
        {
            var step1ViewModel = _stepViewModels[0] as TemplateSetSelectionViewModel;
            var step3ViewModel = _stepViewModels[2] as PlaceholderInputViewModel;
            var step4ViewModel = _stepViewModels[3] as OutputFolderSelectionViewModel;
            var step5ViewModel = _stepViewModels[4] as ProcessingResultsViewModel;
            
            if (step1ViewModel?.SelectedTemplateSet != null && 
                step3ViewModel != null && 
                step4ViewModel?.SelectedFolderPath != null && 
                step5ViewModel != null)
            {
                step5ViewModel.SetProcessingData(
                    step1ViewModel.SelectedTemplateSet.TemplateSetInfo.Path,
                    step4ViewModel.SelectedFolderPath,
                    step3ViewModel.GetReplacementMapping());
            }
        }
    }
}