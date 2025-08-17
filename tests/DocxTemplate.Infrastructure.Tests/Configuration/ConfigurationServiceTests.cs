using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DocxTemplate.Core.Exceptions;
using DocxTemplate.Infrastructure.Configuration;

namespace DocxTemplate.Infrastructure.Tests.Configuration;

public class ConfigurationServiceTests
{
    private readonly IConfigurationService _configurationService;
    private readonly IConfiguration _configuration;

    public ConfigurationServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Application:FileSystem:DefaultEncoding"] = "UTF-8",
            ["Application:FileSystem:MaxFileSizeMB"] = "50",
            ["Application:FileSystem:CreateBackupsOnReplace"] = "true",
            ["Application:Templates:PlaceholderPattern"] = @"\{\{.*?\}\}",
            ["Application:Templates:SupportedExtensions:0"] = ".docx",
            ["Application:Logging:LogLevel"] = "Information",
            ["Application:Performance:IoTimeoutSeconds"] = "30",
            ["ConnectionStrings:Default"] = "test-connection-string"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.Configure<ApplicationSettings>(_configuration.GetSection("Application"));
        services.AddSingleton(_configuration);
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        var serviceProvider = services.BuildServiceProvider();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
    }

    [Fact]
    public void GetSettings_ReturnsApplicationSettings()
    {
        // act
        var result = _configurationService.GetSettings();

        // assert
        result.Should().NotBeNull();
        result.FileSystem.DefaultEncoding.Should().Be("UTF-8");
        result.FileSystem.MaxFileSizeMB.Should().Be(50);
        result.FileSystem.CreateBackupsOnReplace.Should().BeTrue();
        result.Templates.PlaceholderPattern.Should().Be(@"\{\{.*?\}\}");
        result.Templates.SupportedExtensions.Should().Contain(".docx");
        result.Logging.LogLevel.Should().Be("Information");
        result.Performance.IoTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void GetSection_WhenSectionExists_ReturnsPopulatedObject()
    {
        // act
        var result = _configurationService.GetSection<FileSystemSettings>("Application:FileSystem");

        // assert
        result.Should().NotBeNull();
        result.DefaultEncoding.Should().Be("UTF-8");
        result.MaxFileSizeMB.Should().Be(50);
        result.CreateBackupsOnReplace.Should().BeTrue();
    }

    [Fact]
    public void GetSection_WhenSectionDoesNotExist_ReturnsDefaultObject()
    {
        // act
        var result = _configurationService.GetSection<FileSystemSettings>("NonExistent:Section");

        // assert
        result.Should().NotBeNull();
        result.DefaultEncoding.Should().Be("UTF-8"); // Default value
    }

    [Fact]
    public void GetConnectionString_WhenExists_ReturnsConnectionString()
    {
        // act
        var result = _configurationService.GetConnectionString("Default");

        // assert
        result.Should().Be("test-connection-string");
    }

    [Fact]
    public void GetConnectionString_WhenDoesNotExist_ReturnsEmptyString()
    {
        // act
        var result = _configurationService.GetConnectionString("NonExistent");

        // assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ValidateConfiguration_WithValidSettings_DoesNotThrow()
    {
        // act & assert
        _configurationService.Invoking(cs => cs.ValidateConfiguration())
            .Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSection_WithInvalidSectionName_ThrowsArgumentException(string sectionName)
    {
        // act & assert
        _configurationService.Invoking(cs => cs.GetSection<FileSystemSettings>(sectionName))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetConnectionString_WithInvalidName_ThrowsArgumentException(string name)
    {
        // act & assert
        _configurationService.Invoking(cs => cs.GetConnectionString(name))
            .Should().Throw<ArgumentException>();
    }
}

public class ApplicationSettingsValidatorTests
{
    private readonly ApplicationSettingsValidator _validator;

    public ApplicationSettingsValidatorTests()
    {
        _validator = new ApplicationSettingsValidator();
    }

    [Fact]
    public void Validate_WithValidSettings_ReturnsSuccess()
    {
        // arrange
        var settings = CreateValidSettings();

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidMaxFileSizeMB_ReturnsFailed()
    {
        // arrange
        var settings = CreateValidSettings();
        settings.FileSystem.MaxFileSizeMB = 0;

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain("FileSystem.MaxFileSizeMB must be greater than 0");
    }

    [Fact]
    public void Validate_WithInvalidEncoding_ReturnsFailed()
    {
        // arrange
        var settings = CreateValidSettings();
        settings.FileSystem.DefaultEncoding = "InvalidEncoding123";

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("is not a valid encoding"));
    }

    [Fact]
    public void Validate_WithInvalidRegexPattern_ReturnsFailed()
    {
        // arrange
        var settings = CreateValidSettings();
        settings.Templates.PlaceholderPattern = "[invalid regex";

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("is not a valid regex"));
    }

    [Fact]
    public void Validate_WithEmptySupportedExtensions_ReturnsFailed()
    {
        // arrange
        var settings = CreateValidSettings();
        settings.Templates.SupportedExtensions = [];

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain("Templates.SupportedExtensions must contain at least one extension");
    }

    [Fact]
    public void Validate_WithInvalidLogLevel_ReturnsFailed()
    {
        // arrange
        var settings = CreateValidSettings();
        settings.Logging.LogLevel = "InvalidLevel";

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Logging.LogLevel must be one of"));
    }

    [Fact]
    public void Validate_WithNegativeBackupRetentionDays_ReturnsFailed()
    {
        // arrange
        var settings = CreateValidSettings();
        settings.FileSystem.BackupRetentionDays = -1;

        // act
        var result = _validator.Validate(null, settings);

        // assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain("FileSystem.BackupRetentionDays must be non-negative");
    }

    private static ApplicationSettings CreateValidSettings()
    {
        return new ApplicationSettings
        {
            FileSystem = new FileSystemSettings
            {
                DefaultEncoding = "UTF-8",
                MaxFileSizeMB = 50,
                CreateBackupsOnReplace = true,
                BackupSuffix = ".backup",
                BackupRetentionDays = 30
            },
            Templates = new TemplateSettings
            {
                PlaceholderPattern = @"\{\{.*?\}\}",
                SupportedExtensions = [".docx"],
                RecursiveSearch = true,
                ExcludeDirectories = ["bin", "obj"],
                MaxConcurrentOperations = 4
            },
            Logging = new LoggingSettings
            {
                LogLevel = "Information",
                EnableFileLogging = false,
                LogFilePath = "logs/test.log",
                EnableConsoleLogging = true
            },
            Performance = new PerformanceSettings
            {
                IoTimeoutSeconds = 30,
                MemoryLimitMB = 512,
                EnableParallelProcessing = true,
                BatchSize = 10
            }
        };
    }
}