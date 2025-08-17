using System.ComponentModel.DataAnnotations;

namespace DocxTemplate.Infrastructure.Configuration;

public class ApplicationSettings
{
    [Required]
    public FileSystemSettings FileSystem { get; set; } = new();
    
    [Required]
    public TemplateSettings Templates { get; set; } = new();
    
    [Required]
    public LoggingSettings Logging { get; set; } = new();
    
    public PerformanceSettings Performance { get; set; } = new();
}

public class FileSystemSettings
{
    [Required]
    public string DefaultEncoding { get; set; } = "UTF-8";
    
    public int MaxFileSizeMB { get; set; } = 50;
    
    public bool CreateBackupsOnReplace { get; set; } = true;
    
    public string BackupSuffix { get; set; } = ".backup";
    
    public int BackupRetentionDays { get; set; } = 30;
}

public class TemplateSettings
{
    [Required]
    public string PlaceholderPattern { get; set; } = @"\{\{.*?\}\}";
    
    [Required]
    public string[] SupportedExtensions { get; set; } = [".docx"];
    
    public bool RecursiveSearch { get; set; } = true;
    
    public string[] ExcludeDirectories { get; set; } = ["bin", "obj", ".git", "node_modules"];
    
    public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    
    public bool EnableFileLogging { get; set; } = false;
    
    public string LogFilePath { get; set; } = "logs/docx-template-{Date}.log";
    
    public bool EnableConsoleLogging { get; set; } = true;
}

public class PerformanceSettings
{
    public int IoTimeoutSeconds { get; set; } = 30;
    
    public int MemoryLimitMB { get; set; } = 512;
    
    public bool EnableParallelProcessing { get; set; } = true;
    
    public int BatchSize { get; set; } = 10;
}