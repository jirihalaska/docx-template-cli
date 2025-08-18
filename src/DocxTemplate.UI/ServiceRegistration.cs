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
        // Register CLI services
        services.AddSingleton<ICliCommandService, CliProcessRunner>();
        services.AddSingleton<ITemplateSetDiscoveryService, TemplateSetDiscoveryService>();
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<WizardViewModel>(provider => new WizardViewModel(provider));
        services.AddTransient<TemplateSetSelectionViewModel>();
        services.AddTransient<PlaceholderDiscoveryViewModel>();
        services.AddTransient<PlaceholderInputViewModel>();
        
        return services;
    }
}