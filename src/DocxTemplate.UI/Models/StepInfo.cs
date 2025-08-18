namespace DocxTemplate.UI.Models;

public class StepInfo
{
    public required string Title { get; init; }
    public bool IsCompleted { get; set; }
    public bool IsActive { get; set; }
}