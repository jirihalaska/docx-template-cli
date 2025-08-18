using System.Threading.Tasks;

namespace DocxTemplate.UI.Services;

public class CliCommandServiceFactory
{
    private readonly ICliExecutableDiscoveryService _discoveryService;

    public CliCommandServiceFactory(ICliExecutableDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public async Task<ICliCommandService> CreateAsync()
    {
        var cliExecutablePath = await _discoveryService.DiscoverCliExecutableAsync();
        return new CliProcessRunner(cliExecutablePath);
    }
}