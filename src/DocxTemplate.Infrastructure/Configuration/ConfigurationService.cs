using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DocxTemplate.Core.Exceptions;

namespace DocxTemplate.Infrastructure.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<ApplicationSettings> _settings;

    public ConfigurationService(IConfiguration configuration, IOptionsMonitor<ApplicationSettings> settings)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public ApplicationSettings GetSettings()
    {
        return _settings.CurrentValue;
    }

    public T GetSection<T>(string sectionName) where T : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        
        var section = _configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            return new T();
        }

        var result = new T();
        section.Bind(result);
        return result;
    }

    public string GetConnectionString(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _configuration.GetConnectionString(name) ?? string.Empty;
    }

    public void ValidateConfiguration()
    {
        var settings = GetSettings();
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(settings);

        if (!Validator.TryValidateObject(settings, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error");
            throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
        }
    }

    public void ReloadConfiguration()
    {
        if (_configuration is IConfigurationRoot configRoot)
        {
            configRoot.Reload();
        }
    }
}

