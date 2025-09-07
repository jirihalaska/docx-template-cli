using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocxTemplate.Core.Services;
using DocxTemplate.Core.ErrorHandling;
using DocxTemplate.Infrastructure.Configuration;
using DocxTemplate.Infrastructure.Services;
using DocxTemplate.Processing;
using DocxTemplate.Processing.Images;
using DocxTemplate.Processing.Interfaces;

namespace DocxTemplate.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configuration
        services.AddSingleton(configuration);
        services.Configure<ApplicationSettings>(configuration.GetSection("Application"));
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // File System Services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        
        // Document Processing
        services.AddScoped<DocumentTraverser>();
        services.AddScoped<PlaceholderReplacementEngine>();

        // Template Services
        services.AddScoped<ITemplateSetService, TemplateSetDiscoveryService>();
        services.AddScoped<ITemplateDiscoveryService, TemplateDiscoveryService>();
        services.AddScoped<IPlaceholderScanService, PlaceholderScanService>();
        services.AddScoped<ITemplateCopyService, TemplateCopyService>();
        services.AddScoped<IPlaceholderReplaceService, PlaceholderReplaceService>();

        // User Preferences
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();

        // Image Services
        services.AddScoped<IImageProcessor, ImageProcessor>();

        // Error Handling
        services.AddScoped<IErrorHandler, ErrorHandler>();

        // Logging
        services.AddLogging();

        // Validation
        services.AddSingleton<IValidateOptions<ApplicationSettings>, ApplicationSettingsValidator>();

        return services;
    }

    public static IServiceCollection AddInfrastructureWithValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        
        // Build temporary service provider for validation
        using var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();
        configService.ValidateConfiguration();

        return services;
    }
}