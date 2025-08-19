using ReactiveUI;

namespace DocxTemplate.UI.Models;

public class StepInfo : ReactiveObject
{
    private bool _isCompleted;
    private bool _isActive;

    public required string Title { get; init; }
    
    public bool IsCompleted
    {
        get => _isCompleted;
        set => this.RaiseAndSetIfChanged(ref _isCompleted, value);
    }
    
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }
}