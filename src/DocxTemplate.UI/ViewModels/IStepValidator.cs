using System.Threading.Tasks;

namespace DocxTemplate.UI.ViewModels;

public interface IStepValidator
{
    Task<bool> ValidateAsync();
    string? ValidationError { get; }
}