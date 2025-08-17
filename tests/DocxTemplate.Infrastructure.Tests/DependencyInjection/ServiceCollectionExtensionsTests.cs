using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplate.Infrastructure.Configuration;
using DocxTemplate.Infrastructure.DependencyInjection;
using DocxTemplate.Infrastructure.Services;

namespace DocxTemplate.Infrastructure.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Application:FileSystem:DefaultEncoding"] = "UTF-8",
            ["Application:FileSystem:MaxFileSizeMB"] = "50",
            ["Application:Templates:PlaceholderPattern"] = @"\{\{.*?\}\}",
            ["Application:Templates:SupportedExtensions:0"] = ".docx",
            ["Application:Logging:LogLevel"] = "Information",
            ["Application:Performance:IoTimeoutSeconds"] = "30",
            ["Logging:LogLevel:Default"] = "Information"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void AddInfrastructure_RegistersAllRequiredServices()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInfrastructure(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        // assert
        serviceProvider.GetService<IConfigurationService>().Should().NotBeNull();
        serviceProvider.GetService<IFileSystemService>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_RegistersConfigurationCorrectly()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInfrastructure(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        // assert
        var configService = serviceProvider.GetService<IConfigurationService>();
        configService.Should().NotBeNull();
        
        var settings = configService!.GetSettings();
        settings.FileSystem.DefaultEncoding.Should().Be("UTF-8");
        settings.FileSystem.MaxFileSizeMB.Should().Be(50);
    }

    [Fact]
    public void AddInfrastructure_RegistersFileSystemService()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInfrastructure(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        // assert
        var fileSystemService = serviceProvider.GetService<IFileSystemService>();
        fileSystemService.Should().NotBeNull();
        fileSystemService.Should().BeOfType<FileSystemService>();
    }

    [Fact]
    public void AddInfrastructureWithValidation_WithValidConfiguration_DoesNotThrow()
    {
        // arrange
        var services = new ServiceCollection();

        // act & assert
        services.Invoking(s => s.AddInfrastructureWithValidation(_configuration))
            .Should().NotThrow();
    }

    [Fact]
    public void AddInfrastructureWithValidation_WithInvalidConfiguration_ThrowsException()
    {
        // arrange
        var invalidConfigData = new Dictionary<string, string?>
        {
            ["Application:FileSystem:MaxFileSizeMB"] = "0", // Invalid: must be > 0
            ["Application:Templates:PlaceholderPattern"] = "[invalid", // Invalid regex
            ["Application:Logging:LogLevel"] = "InvalidLevel" // Invalid log level
        };

        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidConfigData)
            .Build();

        var services = new ServiceCollection();

        // act & assert
        services.Invoking(s => s.AddInfrastructureWithValidation(invalidConfiguration))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void AddInfrastructure_WithNullServices_ThrowsArgumentNullException()
    {
        // arrange
        IServiceCollection? services = null;

        // act & assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInfrastructure(_configuration));
    }

    [Fact]
    public void AddInfrastructure_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // arrange
        var services = new ServiceCollection();

        // act & assert
        services.Invoking(s => s.AddInfrastructure(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddInfrastructure_RegistersServicesAsSingleton()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInfrastructure(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        // assert
        var configService1 = serviceProvider.GetService<IConfigurationService>();
        var configService2 = serviceProvider.GetService<IConfigurationService>();
        configService1.Should().BeSameAs(configService2);

        var fileSystemService1 = serviceProvider.GetService<IFileSystemService>();
        var fileSystemService2 = serviceProvider.GetService<IFileSystemService>();
        fileSystemService1.Should().BeSameAs(fileSystemService2);
    }
}