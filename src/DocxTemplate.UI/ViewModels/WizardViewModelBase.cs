using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using DocxTemplate.UI.Models;
using ReactiveUI;
using Microsoft.Extensions.DependencyInjection;

namespace DocxTemplate.UI.ViewModels;

/// <summary>
/// Base class for wizard ViewModels providing common wizard functionality
/// </summary>
public abstract class WizardViewModelBase : ViewModelBase
{
    protected readonly IServiceProvider ServiceProvider;
    protected List<UserControl> StepViews = new();
    protected List<StepViewModelBase> StepViewModels = new();
    
    private int _currentStep = 0;
    private bool _canAdvanceToNextStep = true;

    protected WizardViewModelBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // Initialize wizard steps
        InitializeSteps();
        InitializeViewModels();
        InitializeViews();
        
        // Set up reactive commands
        var canGoBack = this.WhenAnyValue(x => x.CurrentStep)
            .Select(step => step > 0 && step < TotalSteps)
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
        if (StepViewModels.Count > 0)
            StepViewModels[0]?.OnStepActivated();
    }

    #region Properties

    public ReadOnlyCollection<StepInfo> Steps { get; protected set; } = null!;

    public int CurrentStep
    {
        get => _currentStep;
        set => this.RaiseAndSetIfChanged(ref _currentStep, value);
    }

    public bool CanAdvanceToNextStep
    {
        get => _canAdvanceToNextStep;
        set
        {
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

    public string StepIndicatorText => $"Krok {CurrentStep + 1} z {TotalSteps}";

    public UserControl CurrentStepContent => StepViews[CurrentStep];

    public abstract string NextButtonText { get; }

    public abstract int TotalSteps { get; }

    public bool IsBackButtonVisible => CurrentStep > 0 && CurrentStep < TotalSteps;
    
    public bool IsNextButtonVisible => CurrentStep < TotalSteps;
    
    public bool IsFinishButtonVisible => CurrentStep == TotalSteps;

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> NextCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Initialize the wizard steps specific to this wizard type
    /// </summary>
    protected abstract void InitializeSteps();

    /// <summary>
    /// Initialize the ViewModels for each step
    /// </summary>
    protected abstract void InitializeViewModels();

    /// <summary>
    /// Initialize the Views for each step
    /// </summary>
    protected abstract void InitializeViews();

    /// <summary>
    /// Prepare data when advancing to the next step
    /// </summary>
    /// <param name="fromStep">The step we're leaving</param>
    /// <param name="toStep">The step we're going to</param>
    protected virtual void PrepareStepTransition(int fromStep, int toStep)
    {
        // Override in derived classes for step-specific preparation
    }

    /// <summary>
    /// Handle wizard completion
    /// </summary>
    protected virtual void OnWizardCompleted()
    {
        // Override in derived classes
    }

    #endregion

    #region Navigation Methods

    protected virtual void GoToNextStep()
    {
        if (CurrentStep >= TotalSteps) return;

        var fromStep = CurrentStep;
        var toStep = CurrentStep + 1;

        // Deactivate current step
        StepViewModels[fromStep]?.OnStepDeactivated();

        // Prepare transition
        PrepareStepTransition(fromStep, toStep);

        // Move to next step
        CurrentStep = toStep;

        // Activate new step if within bounds
        if (toStep < StepViewModels.Count)
        {
            StepViewModels[toStep]?.OnStepActivated();
        }

        UpdateStepStates(CurrentStep);
    }

    protected virtual void GoToPreviousStep()
    {
        if (CurrentStep <= 0) return;

        var fromStep = CurrentStep;
        var toStep = CurrentStep - 1;

        // Deactivate current step
        StepViewModels[fromStep]?.OnStepDeactivated();

        // Move to previous step
        CurrentStep = toStep;

        // Activate previous step
        StepViewModels[toStep]?.OnStepActivated();

        UpdateStepStates(CurrentStep);
    }

    protected virtual void FinishWizard()
    {
        OnWizardCompleted();
    }

    protected void UpdateStepStates(int currentStep)
    {
        // Update step indicator
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsActive = i == currentStep;
            Steps[i].IsCompleted = i < currentStep;
        }

        // Update validation state
        if (currentStep < StepViewModels.Count)
        {
            var currentStepViewModel = StepViewModels[currentStep];
            CanAdvanceToNextStep = currentStepViewModel?.ValidateStep() ?? true;

            // Subscribe to validation changes for current step
            if (currentStepViewModel != null)
            {
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => currentStepViewModel.PropertyChanged += h,
                    h => currentStepViewModel.PropertyChanged -= h)
                    .Where(e => e.EventArgs.PropertyName == nameof(StepViewModelBase.IsValid))
                    .Subscribe(_ => CanAdvanceToNextStep = currentStepViewModel.IsValid);
            }
        }

        // Notify UI of property changes
        this.RaisePropertyChanged(nameof(CurrentStepTitle));
        this.RaisePropertyChanged(nameof(StepIndicatorText));
        this.RaisePropertyChanged(nameof(CurrentStepContent));
        this.RaisePropertyChanged(nameof(NextButtonText));
        this.RaisePropertyChanged(nameof(IsBackButtonVisible));
        this.RaisePropertyChanged(nameof(IsNextButtonVisible));
        this.RaisePropertyChanged(nameof(IsFinishButtonVisible));
    }

    #endregion
}