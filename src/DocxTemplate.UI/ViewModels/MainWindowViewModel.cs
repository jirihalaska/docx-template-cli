using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _title = "Procesor šablon DOCX";
    private string _statusText = "Připraveno";

    public MainWindowViewModel(WizardCoordinatorViewModel wizardCoordinator)
    {
        WizardCoordinator = wizardCoordinator ?? throw new ArgumentNullException(nameof(wizardCoordinator));
        
        // Update status text based on wizard coordinator state
        this.WhenAnyValue(
                x => x.WizardCoordinator.ShowModeSelection,
                x => x.WizardCoordinator.SelectedMode,
                x => x.WizardCoordinator.CurrentWizard)
            .Select(_ => WizardCoordinator.GetStatusText())
            .Subscribe(statusText => StatusText = statusText);
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public WizardCoordinatorViewModel WizardCoordinator { get; }
}
