namespace DocxTemplate.Core.Models;

/// <summary>
/// User preferences and settings for the application
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Last used directory for image file selection
    /// </summary>
    public string? LastUsedImageDirectory { get; set; }
    
    /// <summary>
    /// Last used directory for output folder selection
    /// </summary>
    public string? LastUsedOutputDirectory { get; set; }
    
    /// <summary>
    /// Last used directory for existing project folder selection
    /// </summary>
    public string? LastUsedProjectDirectory { get; set; }
}