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
        services.AddTransient<CliCommandBuilder>();
        services.AddTransient<ITemplateSetDiscoveryService, TemplateSetDiscoveryService>();
        
        // Register lazy CLI command service - discovery will happen on first use, not during startup
        services.AddSingleton<ICliCommandService, LazyCliCommandService>();
        
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