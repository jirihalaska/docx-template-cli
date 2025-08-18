using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplate.UI.Views;
using DocxTemplate.UI.ViewModels;
using DocxTemplate.UI;

namespace DocxTemplate.EndToEnd.Tests.GUI.Infrastructure;

/// <summary>
/// Base class for end-to-end integration tests focusing on CLI-GUI integration workflow
/// </summary>
public abstract class GuiTestBase : IAsyncLifetime
{
    protected string TestDataDirectory { get; private set; } = null!;
    protected string TempOutputDirectory { get; private set; } = null!;
    
    public virtual async Task InitializeAsync()
    {
        // Set up test directories
        var testAssemblyDir = Path.GetDirectoryName(typeof(GuiTestBase).Assembly.Location)!;
        TestDataDirectory = Path.Combine(testAssemblyDir, "..", "..", "..", "..", "data", "e2e-documents");
        TempOutputDirectory = Path.Combine(Path.GetTempPath(), "DocxTemplate.Tests", Guid.NewGuid().ToString());
        
        Directory.CreateDirectory(TempOutputDirectory);
        await Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        // Clean up temp directory
        if (Directory.Exists(TempOutputDirectory))
        {
            try
            {
                Directory.Delete(TempOutputDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        await Task.CompletedTask;
    }
    
    protected MainWindow CreateMainWindow()
    {
        // Initialize Avalonia if not already initialized
        if (Application.Current == null)
        {
            var app = new TestApp();
            app.Initialize();
        }
        
        // Setup dependency injection same as the real app
        var services = new ServiceCollection();
        services.RegisterServices();
        var serviceProvider = services.BuildServiceProvider();
        
        var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        
        return new MainWindow
        {
            DataContext = mainWindowViewModel
        };
    }
    
    protected async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var endTime = DateTime.Now + timeout;
        while (DateTime.Now < endTime)
        {
            if (condition())
                return;
            await Task.Delay(100);
        }
        throw new TimeoutException($"Condition not met within {timeout}");
    }
}