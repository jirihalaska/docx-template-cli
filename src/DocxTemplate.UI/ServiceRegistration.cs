using System;
using Microsoft.Extensions.DependencyInjection;
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
        // Register CLI services as transient to avoid startup deadlock
        services.AddTransient<ICliExecutableDiscoveryService, CliExecutableDiscoveryService>();
        services.AddTransient<CliCommandServiceFactory>();
        services.AddTransient<ITemplateSetDiscoveryService, TemplateSetDiscoveryService>();
        
        // Register ICliCommandService factory that won't block startup
        services.AddTransient<ICliCommandService>(provider =>
        {
            var factory = provider.GetRequiredService<CliCommandServiceFactory>();
            return factory.CreateAsync().GetAwaiter().GetResult();
        });
        
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