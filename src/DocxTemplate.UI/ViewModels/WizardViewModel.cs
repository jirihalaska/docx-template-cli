using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using DocxTemplate.UI.Models;
using DocxTemplate.UI.Views.Steps;
using Avalonia.Controls;

namespace DocxTemplate.UI.ViewModels;

public class WizardViewModel : ViewModelBase
{
    private int _currentStep = 1;
    private const int TotalSteps = 5;
    private readonly List<UserControl> _stepViews;

    public WizardViewModel()
    {
        var stepList = new List<StepInfo>
        {
            new() { Title = "Vyberte sadu šablon", IsActive = true },
            new() { Title = "Nalezení zástupných symbolů" },
            new() { Title = "Zadání hodnot zástupných symbolů" },
            new() { Title = "Výběr výstupní složky" },
            new() { Title = "Zpracování a výsledky" }
        };
        Steps = new ReadOnlyCollection<StepInfo>(stepList);

        _stepViews = new List<UserControl>
        {
            new Step1TemplateSelectionView(),
            new Step2PlaceholderDiscoveryView(),
            new Step3PlaceholderInputView(),
            new Step4OutputSelectionView(),
            new Step5ProcessingResultsView()
        };

        var canGoBack = this.WhenAnyValue(x => x.CurrentStep, step => step > 1);
        var canGoNext = this.WhenAnyValue(x => x.CurrentStep, step => step < TotalSteps)
            .CombineLatest(this.WhenAnyValue(x => x.CanAdvanceToNextStep), (hasNext, canAdvance) => hasNext && canAdvance);

        NextCommand = ReactiveCommand.Create(GoToNextStep, canGoNext);
        BackCommand = ReactiveCommand.Create(GoToPreviousStep, canGoBack);

        this.WhenAnyValue(x => x.CurrentStep)
            .Subscribe(UpdateStepStates);
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
            Steps[CurrentStep - 1].IsCompleted = true;
            CurrentStep++;
        }
    }

    private void GoToPreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    private void UpdateStepStates(int currentStep)
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsActive = (i + 1) == currentStep;
        }

        this.RaisePropertyChanged(nameof(CurrentStepTitle));
        this.RaisePropertyChanged(nameof(StepIndicatorText));
        this.RaisePropertyChanged(nameof(CurrentStepContent));
    }
}