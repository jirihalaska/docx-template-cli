using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using DocxTemplate.UI;

namespace DocxTemplate.EndToEnd.Tests;

/// <summary>
/// Test application for Avalonia headless testing
/// </summary>
public class TestApp : App
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Don't set main window in test mode - let tests control window creation
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Handle single view platform if needed
        }

        base.OnFrameworkInitializationCompleted();
    }
}