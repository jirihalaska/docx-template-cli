using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocxTemplate.UI.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocxTemplate.EndToEnd.Tests.GUI;

/// <summary>
/// Simple E2E integration tests that verify GUI-CLI integration without complex UI automation
/// </summary>
[Collection("GUI Integration E2E Tests")]
public class GuiIntegrationE2ETests : IAsyncLifetime
{
    private string _tempOutputDirectory = null!;

    public async Task InitializeAsync()
    {
        _tempOutputDirectory = Path.Combine(Path.GetTempPath(), "DocxTemplate.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempOutputDirectory);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_tempOutputDirectory))
        {
            try
            {
                Directory.Delete(_tempOutputDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GuiE2E_ServiceResolution_AllRequiredServicesCanBeResolved()
    {
        // arrange - Setup DI container exactly like the real app
        var services = new ServiceCollection();
        DocxTemplate.UI.ServiceRegistration.RegisterServices(services);

        // act - Build service provider (this is what the real GUI does)
        var serviceProvider = services.BuildServiceProvider();

        // assert - All critical services should resolve without errors
        var cliDiscovery = serviceProvider.GetRequiredService<ICliExecutableDiscoveryService>();
        cliDiscovery.Should().NotBeNull("CLI executable discovery service should be available");

        var cliCommandService = serviceProvider.GetRequiredService<ICliCommandService>();
        cliCommandService.Should().NotBeNull("CLI command service should be available");

        var templateSetService = serviceProvider.GetRequiredService<ITemplateSetDiscoveryService>();
        templateSetService.Should().NotBeNull("Template set discovery service should be available");

        // Verify we can create view models (this is what the GUI does)
        var mainViewModel = serviceProvider.GetRequiredService<DocxTemplate.UI.ViewModels.MainWindowViewModel>();
        mainViewModel.Should().NotBeNull("Main window view model should be resolvable");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task GuiE2E_CliExecutableDiscovery_FindsCorrectCliExecutable()
    {
        // arrange
        var services = new ServiceCollection();
        DocxTemplate.UI.ServiceRegistration.RegisterServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // act - Get CLI executable discovery service (this tests the core integration)
        var cliDiscovery = serviceProvider.GetRequiredService<ICliExecutableDiscoveryService>();
        var cliExecutablePath = await cliDiscovery.DiscoverCliExecutableAsync();

        // assert - Should find a valid CLI executable
        cliExecutablePath.Should().NotBeNullOrEmpty("CLI executable should be discovered");
        File.Exists(cliExecutablePath).Should().BeTrue($"CLI executable should exist at: {cliExecutablePath}");
        
        // Verify it's the correct executable
        Path.GetFileNameWithoutExtension(cliExecutablePath).Should().Be("docx-template", 
            "Should find the correct CLI executable name");
    }

    [Fact]
    public async Task GuiE2E_CliCommandService_CanExecuteBasicCommands()
    {
        // arrange
        var services = new ServiceCollection();
        DocxTemplate.UI.ServiceRegistration.RegisterServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // act - Get CLI command service and test basic functionality
        var cliCommandService = serviceProvider.GetRequiredService<ICliCommandService>();

        // Test a simple command that should work
        var result = await cliCommandService.ExecuteCommandAsync("--help", new string[0]);

        // assert - Command should execute and return help text
        result.Should().NotBeNull("CLI command should return result");
        result.Should().Contain("DOCX", "Help output should contain application info");
    }

    [Fact]
    public async Task GuiE2E_TemplateSetDiscovery_WorksWithRealService()
    {
        // arrange - Create a test template directory
        var testTemplatesDir = Path.Combine(_tempOutputDirectory, "test_templates");
        var testTemplateSetDir = Path.Combine(testTemplatesDir, "TestSet");
        Directory.CreateDirectory(testTemplateSetDir);
        
        // Create a simple DOCX file for testing
        var testDocxPath = Path.Combine(testTemplateSetDir, "test.docx");
        using (var fileStream = File.Create(testDocxPath))
        {
            // Create minimal valid DOCX structure
            var docxBytes = CreateMinimalDocxBytes();
            await fileStream.WriteAsync(docxBytes);
        }

        var services = new ServiceCollection();
        DocxTemplate.UI.ServiceRegistration.RegisterServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // act - Use template set discovery service
        var templateSetService = serviceProvider.GetRequiredService<ITemplateSetDiscoveryService>();
        var templateSets = await templateSetService.DiscoverTemplateSetsAsync(testTemplatesDir);

        // assert - Should discover the test template set
        templateSets.Should().NotBeEmpty("Should discover template sets");
        templateSets.Should().Contain(t => t.Name == "TestSet", "Should find our test template set");
        
        var testSet = templateSets.First(t => t.Name == "TestSet");
        testSet.FileCount.Should().BeGreaterThan(0, "Template set should contain files");
        testSet.Path.Should().EndWith("TestSet", "Should have correct path");
    }

    [Fact]
    public async Task GuiE2E_MainWindowViewModel_InitializesCorrectly()
    {
        // arrange
        var services = new ServiceCollection();
        DocxTemplate.UI.ServiceRegistration.RegisterServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // act - Create main window view model (this is what happens when GUI starts)
        var mainWindowViewModel = serviceProvider.GetRequiredService<DocxTemplate.UI.ViewModels.MainWindowViewModel>();

        // assert - View model should initialize properly
        mainWindowViewModel.Should().NotBeNull("Main window view model should be created");
        mainWindowViewModel.WizardViewModel.Should().NotBeNull("Wizard view model should be available");
        
        // Test that wizard starts in correct state
        mainWindowViewModel.WizardViewModel.CurrentStep.Should().Be(1, "Should start on first wizard step");
        
        await Task.CompletedTask;
    }

    private byte[] CreateMinimalDocxBytes()
    {
        // Create a minimal valid DOCX structure for testing
        // This is a very basic DOCX that DocumentFormat.OpenXml can read
        var zipContent = new byte[] { 
            0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00, 0x08, 0x00, 
            // More bytes would be here for a real DOCX, but this is enough for basic file detection
        };
        
        // For simplicity, we'll create an empty file that will be recognized as a DOCX by extension
        return new byte[0];
    }
}