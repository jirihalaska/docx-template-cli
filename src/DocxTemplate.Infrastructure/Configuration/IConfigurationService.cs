namespace DocxTemplate.Infrastructure.Configuration;

public interface IConfigurationService
{
    ApplicationSettings GetSettings();
    T GetSection<T>(string sectionName) where T : class, new();
    string GetConnectionString(string name);
    void ValidateConfiguration();
    void ReloadConfiguration();
}