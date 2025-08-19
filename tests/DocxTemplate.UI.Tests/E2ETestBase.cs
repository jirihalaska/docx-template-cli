using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplate.UI;
using Xunit.Abstractions;

namespace DocxTemplate.UI.Tests;

/// <summary>
/// Base class for E2E UI tests providing test infrastructure
/// </summary>
public abstract class E2ETestBase : IDisposable
{
    protected readonly ITestOutputHelper TestOutput;
    private readonly string _testOutputDirectory;
    private bool _disposed;

    protected E2ETestBase(ITestOutputHelper testOutput)
    {
        TestOutput = testOutput ?? throw new ArgumentNullException(nameof(testOutput));
        _testOutputDirectory = SetupTestOutput();
    }

    /// <summary>
    /// Sets up the test output directory with timestamp
    /// </summary>
    /// <returns>Path to the test output directory</returns>
    protected string SetupTestOutput()
    {
        var testName = GetType().Name;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var testOutput = Path.Combine("./test-output/e2e", testName, timestamp);
        
        // Clear previous runs for this test
        var testDir = Path.Combine("./test-output/e2e", testName);
        if (Directory.Exists(testDir))
        {
            TestOutput.WriteLine($"Clearing previous test runs in: {testDir}");
            Directory.Delete(testDir, recursive: true);
        }
            
        Directory.CreateDirectory(testOutput);
        TestOutput.WriteLine($"Test output directory: {Path.GetFullPath(testOutput)}");
        return testOutput;
    }

    /// <summary>
    /// Gets the test output directory path
    /// </summary>
    protected string TestOutputDirectory => _testOutputDirectory;

    /// <summary>
    /// Creates and configures the Avalonia application for testing
    /// </summary>
    protected Application CreateTestApplication()
    {
        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting()
            .Instance!;
    }

    /// <summary>
    /// Sets up a service collection with all required dependencies
    /// </summary>
    protected IServiceProvider SetupServices()
    {
        var services = new ServiceCollection();
        
        // Create basic configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .Build();

        // Register services using the UI service registration
        services.RegisterServices();
        
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a parameters file documenting test inputs
    /// </summary>
    protected async Task CreateTestParametersFile(object parameters, string fileName = "test-parameters.json")
    {
        var filePath = Path.Combine(_testOutputDirectory, fileName);
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var parametersInfo = new
        {
            TestName = GetType().Name,
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Parameters = parameters,
            OutputDirectory = Path.GetFullPath(_testOutputDirectory)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(parametersInfo, jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        
        TestOutput.WriteLine($"Test parameters saved to: {filePath}");
    }

    /// <summary>
    /// Gets the path to the templates directory
    /// </summary>
    protected string GetTemplatesPath()
    {
        // Templates are copied to the test output directory during build
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "templates");
        
        if (!Directory.Exists(templatesPath))
        {
            throw new DirectoryNotFoundException($"Templates directory not found at: {templatesPath}. Make sure the test project builds successfully.");
        }
        
        return templatesPath;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Disposal logic if needed
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}