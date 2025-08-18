using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

public interface ICliCommandService
{
    Task<string> ExecuteCommandAsync(string command, string[] arguments);
}