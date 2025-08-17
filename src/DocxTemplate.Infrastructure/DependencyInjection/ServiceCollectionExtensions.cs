using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocxTemplate.Infrastructure.Configuration;
using DocxTemplate.Infrastructure.Services;

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