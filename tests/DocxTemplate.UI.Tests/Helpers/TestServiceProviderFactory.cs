using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplate.UI;

namespace DocxTemplate.UI.Tests.Helpers;

/// <summary>
/// Factory for creating test service providers
/// </summary>
public static class TestServiceProviderFactory
{
    /// <summary>
    /// Creates a service provider with all required dependencies for testing
    /// </summary>
    public static IServiceProvider Create()
    {
        var services = new ServiceCollection();

        // Create basic configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .Build();

        // Register services using the UI service registration
        services.RegisterServices();

        return services.BuildServiceProvider();
    }
}