# Avalonia UI Best Practices
**DOCX Template CLI GUI Implementation Guide**  
*Created: 2025-01-18*  
*Focus: Simple, Maintainable, Cross-Platform*

---

## 🎯 Quick Start Checklist

```bash
# Create the UI project
dotnet new avalonia.mvvm -n DocxTemplate.UI -o src/DocxTemplate.UI
cd src/DocxTemplate.UI

# Add references to your existing projects
dotnet add reference ../DocxTemplate.Core
dotnet add reference ../DocxTemplate.Infrastructure

# Add essential Avalonia packages
dotnet add package Avalonia.Controls.DataGrid
dotnet add package MessageBox.Avalonia
dotnet add package Avalonia.Diagnostics --condition "DEBUG"
```

---

## 📁 Project Structure Best Practices

### Recommended Structure for Your Simple UI

```
DocxTemplate.UI/
├── Views/                      # XAML views
│   ├── MainWindow.axaml       # Main application window
│   ├── MainWindow.axaml.cs    # Code-behind (minimal)
│   └── Dialogs/
│       └── ProgressDialog.axaml
├── ViewModels/                 # Business logic
│   ├── MainWindowViewModel.cs # Main VM with your CLI service calls
│   ├── ViewModelBase.cs       # Base class for property notification
│   └── ProgressViewModel.cs
├── Controls/                   # Reusable controls (if needed)
│   └── PlaceholderInput.axaml
├── Services/                   # UI-specific services
│   ├── IDialogService.cs      # Abstract dialog operations
│   └── DialogService.cs       # Platform-specific implementation
├── Converters/                 # XAML value converters
│   └── BoolToVisibilityConverter.cs
├── Assets/                     # Icons, images
│   └── icon.ico
├── App.axaml                   # Application resources
├── App.axaml.cs               # Application startup
└── Program.cs                  # Entry point
```

### Why This Structure?
- **Separation of Concerns**: Views handle UI, ViewModels handle logic
- **Testability**: ViewModels can be unit tested without UI
- **Simplicity**: No over-engineering for your basic needs

---

## 🏗️ MVVM Pattern for Simplicity

### 1. Simple ViewModel Base

```csharp
using ReactiveUI;

namespace DocxTemplate.UI.ViewModels;

public class ViewModelBase : ReactiveObject
{
    // That's it! ReactiveUI handles property notifications
}
```

### 2. Main ViewModel Connecting to Your CLI Services

```csharp
public class MainWindowViewModel : ViewModelBase
{
    private readonly ITemplateDiscoveryService _discoveryService;
    private readonly IPlaceholderScanService _scanService;
    private readonly ITemplateCopyService _copyService;
    
    private string _selectedPath;
    private string _statusMessage;
    private bool _isProcessing;
    
    public MainWindowViewModel(
        ITemplateDiscoveryService discoveryService,
        IPlaceholderScanService scanService,
        ITemplateCopyService copyService)
    {
        _discoveryService = discoveryService;
        _scanService = scanService;
        _copyService = copyService;
        
        // Setup commands
        BrowseCommand = ReactiveCommand.CreateFromTask(BrowseForFolder);
        ProcessCommand = ReactiveCommand.CreateFromTask(
            ProcessTemplates,
            this.WhenAnyValue(x => x.SelectedPath, path => !string.IsNullOrEmpty(path))
        );
    }
    
    public string SelectedPath
    {
        get => _selectedPath;
        set => this.RaiseAndSetIfChanged(ref _selectedPath, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }
    
    public bool IsProcessing
    {
        get => _isProcessing;
        set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
    }
    
    public ReactiveCommand<Unit, Unit> BrowseCommand { get; }
    public ReactiveCommand<Unit, Unit> ProcessCommand { get; }
    
    private async Task BrowseForFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Template Folder"
        };
        
        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
            
        var result = await dialog.ShowAsync(window);
        if (!string.IsNullOrEmpty(result))
        {
            SelectedPath = result;
        }
    }
    
    private async Task ProcessTemplates()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Discovering templates...";
            
            // Call your existing CLI services
            var templates = await Task.Run(() => 
                _discoveryService.DiscoverTemplates(SelectedPath));
            
            StatusMessage = $"Found {templates.Count} templates. Scanning for placeholders...";
            
            var placeholders = await Task.Run(() => 
                _scanService.ScanTemplates(templates));
            
            StatusMessage = $"Found {placeholders.Count} unique placeholders. Ready to process.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
```

### 3. Simple XAML View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:DocxTemplate.UI.ViewModels"
        x:Class="DocxTemplate.UI.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="DOCX Template Processor"
        Width="600" Height="400">
    
    <Grid RowDefinitions="Auto,*,Auto" Margin="20">
        
        <!-- Input Section -->
        <StackPanel Grid.Row="0" Spacing="10">
            <TextBlock Text="Template Folder:" />
            <Grid ColumnDefinitions="*,Auto" Spacing="10">
                <TextBox Grid.Column="0" 
                         Text="{Binding SelectedPath}" 
                         IsReadOnly="True"
                         Watermark="Select a folder containing templates..." />
                <Button Grid.Column="1" 
                        Content="Browse..." 
                        Command="{Binding BrowseCommand}" />
            </Grid>
        </StackPanel>
        
        <!-- Status/Output Section -->
        <Border Grid.Row="1" 
                Margin="0,20" 
                BorderBrush="Gray" 
                BorderThickness="1"
                CornerRadius="4">
            <ScrollViewer Padding="10">
                <TextBlock Text="{Binding StatusMessage}" 
                           TextWrapping="Wrap" />
            </ScrollViewer>
        </Border>
        
        <!-- Action Section -->
        <StackPanel Grid.Row="2" 
                    Orientation="Horizontal" 
                    HorizontalAlignment="Right"
                    Spacing="10">
            <ProgressBar IsVisible="{Binding IsProcessing}"
                         IsIndeterminate="True"
                         Width="100" />
            <Button Content="Process Templates" 
                    Command="{Binding ProcessCommand}"
                    IsEnabled="{Binding !IsProcessing}" />
        </StackPanel>
        
    </Grid>
</Window>
```

---

## 📂 File Dialog Best Practices

### DO: Use Avalonia's Built-in Dialogs

```csharp
// Folder selection
var dialog = new OpenFolderDialog
{
    Title = "Select Template Folder",
    Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
};

// File selection (for JSON mapping files)
var fileDialog = new OpenFileDialog
{
    Title = "Select Replacement Mapping",
    Filters = new List<FileDialogFilter>
    {
        new() { Name = "JSON Files", Extensions = { "json" } },
        new() { Name = "All Files", Extensions = { "*" } }
    },
    AllowMultiple = false
};

// Save dialog (for output)
var saveDialog = new SaveFileDialog
{
    Title = "Save Processed Templates",
    DefaultExtension = "docx",
    Filters = new List<FileDialogFilter>
    {
        new() { Name = "Word Documents", Extensions = { "docx" } }
    }
};
```

### DON'T: Use System.Windows.Forms or native dialogs
- Breaks cross-platform compatibility
- Requires platform-specific code

---

## ⚡ Async/Await for Long Operations

### Best Practice: Always Use Task.Run for CPU-Bound Work

```csharp
public class TemplateProcessingViewModel : ViewModelBase
{
    private CancellationTokenSource _cancellationTokenSource;
    
    public async Task ProcessLargeTemplateSet(string path)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        
        try
        {
            // Update UI on UI thread
            StatusMessage = "Starting processing...";
            IsProcessing = true;
            
            // Move CPU-intensive work off UI thread
            var result = await Task.Run(async () =>
            {
                var templates = _discoveryService.DiscoverTemplates(path);
                token.ThrowIfCancellationRequested();
                
                var placeholders = _scanService.ScanTemplates(templates);
                token.ThrowIfCancellationRequested();
                
                // For progress updates, dispatch back to UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"Processing {templates.Count} templates...";
                });
                
                return _copyService.CopyTemplates(templates, outputPath);
            }, token);
            
            // Back on UI thread automatically after await
            StatusMessage = $"Completed! Processed {result.ProcessedCount} files.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
        }
    }
    
    public void CancelProcessing()
    {
        _cancellationTokenSource?.Cancel();
    }
}
```

### Progress Reporting Pattern

```csharp
public async Task ProcessWithProgress()
{
    var progress = new Progress<int>(percent =>
    {
        // This executes on UI thread
        ProgressValue = percent;
        StatusMessage = $"Processing... {percent}%";
    });
    
    await Task.Run(() =>
    {
        for (int i = 0; i <= 100; i++)
        {
            // Simulate work
            Thread.Sleep(50);
            
            // Report progress - automatically marshaled to UI thread
            ((IProgress<int>)progress).Report(i);
        }
    });
}
```

---

## 🚀 Deployment Best Practices

### 1. Self-Contained Deployment (Recommended for Simplicity)

```xml
<!-- In your .csproj -->
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <ApplicationIcon>Assets\icon.ico</ApplicationIcon>
</PropertyGroup>
```

```bash
# Windows deployment
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# macOS deployment  
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# macOS ARM (M1/M2)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

### 2. Create Platform-Specific Installers

**Windows (using Inno Setup):**
```ini
[Setup]
AppName=DOCX Template Processor
AppVersion=2.0
DefaultDirName={pf}\DocxTemplateProcessor
DefaultGroupName=DOCX Template Processor
OutputBaseFilename=DocxTemplateSetup
Compression=lzma2
SolidCompression=yes

[Files]
Source: "publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\DOCX Template Processor"; Filename: "{app}\DocxTemplate.UI.exe"
```

**macOS (create .app bundle):**
```bash
# Structure
DocxTemplate.app/
├── Contents/
│   ├── Info.plist
│   ├── MacOS/
│   │   └── DocxTemplate.UI (your executable)
│   └── Resources/
│       └── icon.icns
```

---

## 💡 Simple UI Tips for Your Use Case

### 1. Keep ViewModels Thin
```csharp
// GOOD: ViewModel just coordinates
public async Task ProcessCommand()
{
    var result = await _templateService.Process(Path);
    StatusMessage = result.Message;
}

// BAD: Business logic in ViewModel
public async Task ProcessCommand()
{
    // Don't put document processing logic here!
    var doc = WordprocessingDocument.Open(path);
    // ... lots of business logic
}
```

### 2. Use Data Binding for Everything
```xml
<!-- GOOD: Declarative binding -->
<TextBox Text="{Binding Path}" />
<Button Command="{Binding ProcessCommand}" />

<!-- BAD: Code-behind event handlers -->
<Button Click="OnButtonClick" />
```

### 3. Simple Error Display
```csharp
public class DialogService : IDialogService
{
    public async Task ShowError(string message, string title = "Error")
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard(title, message, ButtonEnum.Ok, Icon.Error);
        await box.ShowAsync();
    }
    
    public async Task<bool> ShowConfirmation(string message, string title = "Confirm")
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard(title, message, ButtonEnum.YesNo, Icon.Question);
        var result = await box.ShowAsync();
        return result == ButtonResult.Yes;
    }
}
```

---

## 🎨 Styling for Simplicity

### Use Default Theme with Minor Tweaks

```xml
<!-- App.axaml -->
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>
    
    <!-- Simple custom styles -->
    <Style Selector="Button">
        <Setter Property="MinWidth" Value="75"/>
        <Setter Property="Padding" Value="10,5"/>
    </Style>
    
    <Style Selector="TextBox">
        <Setter Property="MinHeight" Value="30"/>
    </Style>
</Application.Styles>
```

---

## 🔧 Dependency Injection Setup

```csharp
// Program.cs
public class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}

// App.axaml.cs
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Simple DI setup
        var services = new ServiceCollection();
        
        // Register your existing services
        services.AddSingleton<ITemplateDiscoveryService, TemplateDiscoveryService>();
        services.AddSingleton<IPlaceholderScanService, PlaceholderScanService>();
        services.AddSingleton<ITemplateCopyService, TemplateCopyService>();
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        var provider = services.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

---

## ⚠️ Common Pitfalls to Avoid

### 1. ❌ Don't Access UI from Background Threads
```csharp
// WRONG
Task.Run(() => {
    StatusTextBlock.Text = "Processing..."; // Will crash!
});

// RIGHT
await Dispatcher.UIThread.InvokeAsync(() => {
    StatusMessage = "Processing..."; // Use binding instead
});
```

### 2. ❌ Don't Use Platform-Specific Code
```csharp
// WRONG
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Windows-specific code
}

// RIGHT - Use Avalonia's abstractions
var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions());
```

### 3. ❌ Don't Overcomplicate for Simple Needs
```csharp
// You DON'T need:
// - Complex navigation frameworks
// - Dependency injection containers (beyond basic)
// - Custom controls for standard inputs
// - Themes beyond the default
// - Complex view locators
```

---

## 📚 Essential Resources

1. **Official Avalonia Documentation**: https://docs.avaloniaui.net/
2. **Avalonia Community Toolkit**: https://github.com/AvaloniaUtils/Avalonia.Controls.ItemsRepeater
3. **Sample Apps**: https://github.com/AvaloniaUI/Avalonia.Samples
4. **XAML Basics**: https://docs.avaloniaui.net/docs/basics/user-interface/introduction-to-xaml

---

## 🚦 Quick Implementation Roadmap

### Day 1: Setup & Basic Window
- Create project structure
- Add references to Core/Infrastructure
- Create basic MainWindow with file picker

### Day 2: Connect to Services
- Implement MainWindowViewModel
- Wire up existing CLI services
- Test basic discovery/scan operations

### Day 3: Polish & Error Handling
- Add progress indicators
- Implement error dialogs
- Add status messages

### Day 4: Deployment
- Create publish profiles
- Test on Windows and macOS
- Create simple installer

### Day 5: Documentation & Handoff
- User guide
- Deployment instructions
- Known issues list

---

*These practices focus on keeping your Avalonia implementation simple, maintainable, and perfectly suited for your basic UI needs. Avoid over-engineering - your use case doesn't need complex patterns!*