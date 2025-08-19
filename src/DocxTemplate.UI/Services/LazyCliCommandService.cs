using System;
using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

/// <summary>
/// Lazy wrapper for CLI command service that defers CLI discovery until first use
/// This prevents blocking the GUI startup with CLI discovery
/// </summary>
public class LazyCliCommandService : ICliCommandService
{
    private readonly ICliExecutableDiscoveryService _discoveryService;
    private readonly Lazy<Task<ICliCommandService>> _cliServiceTask;

    public LazyCliCommandService(ICliExecutableDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _cliServiceTask = new Lazy<Task<ICliCommandService>>(CreateCliServiceAsync);
    }

    public async Task<string> ExecuteCommandAsync(string command, string[] arguments)
    {
        var cliService = await _cliServiceTask.Value;
        return await cliService.ExecuteCommandAsync(command, arguments);
    }

    private async Task<ICliCommandService> CreateCliServiceAsync()
    {
        try
        {
            var cliExecutablePath = await _discoveryService.DiscoverCliExecutableAsync();
            return new CliProcessRunner(cliExecutablePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize CLI command service: {ex.Message}", ex);
        }
    }
}