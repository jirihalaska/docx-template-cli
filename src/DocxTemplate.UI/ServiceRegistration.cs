using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplate.Core.Services;
using DocxTemplate.Infrastructure.DependencyInjection;
using DocxTemplate.Infrastructure.Services;
using DocxTemplate.UI.Services;
using DocxTemplate.UI.ViewModels;

namespace DocxTemplate.UI;

/// <summary>
/// Service registration for dependency injection container
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers all services and ViewModels for the UI application
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .Build();

        // Register Infrastructure services directly (no CLI dependency)
        services.AddInfrastructure(configuration);
        
        // Register UI-specific template set discovery service
        services.AddTransient<ITemplateSetDiscoveryService, UI.Services.TemplateSetDiscoveryService>();
        
        // Register ViewModels
        services.AddTransient<WizardViewModel>(provider => new WizardViewModel(provider));
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<TemplateSetSelectionViewModel>();
        services.AddTransient<PlaceholderDiscoveryViewModel>();
        services.AddTransient<PlaceholderInputViewModel>();
        services.AddTransient<OutputFolderSelectionViewModel>();
        services.AddTransient<ProcessingResultsViewModel>();
        
        return services;
    }
}