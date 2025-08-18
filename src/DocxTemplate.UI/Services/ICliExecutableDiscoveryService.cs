using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

public interface ICliExecutableDiscoveryService
{
    /// <summary>
    /// Discovers the CLI executable path in the same directory as the GUI executable.
    /// </summary>
    /// <returns>The full path to the CLI executable if found.</returns>
    /// <exception cref="CliExecutableNotFoundException">Thrown when CLI executable is not found in the expected location.</exception>
    Task<string> DiscoverCliExecutableAsync();
    
    /// <summary>
    /// Validates that the discovered CLI executable is functional.
    /// </summary>
    /// <param name="cliExecutablePath">The path to the CLI executable to validate.</param>
    /// <returns>True if the CLI executable responds correctly, false otherwise.</returns>
    Task<bool> ValidateCliExecutableAsync(string cliExecutablePath);
}