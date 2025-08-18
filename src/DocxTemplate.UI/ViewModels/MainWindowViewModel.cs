using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _title = "Procesor šablon DOCX";
    private string _statusText = "Připraveno";

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
}
