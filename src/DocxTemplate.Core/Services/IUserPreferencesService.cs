namespace DocxTemplate.Core.Services;

/// <summary>
/// Service for managing user preferences and settings
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets the last used directory for image file selection
    /// </summary>
    /// <returns>Directory path or null if not set</returns>
    Task<string?> GetLastUsedImageDirectoryAsync();
    
    /// <summary>
    /// Sets the last used directory for image file selection
    /// </summary>
    /// <param name="directoryPath">Directory path to save</param>
    Task SetLastUsedImageDirectoryAsync(string directoryPath);
    
    /// <summary>
    /// Gets the last used directory for output folder selection
    /// </summary>
    /// <returns>Directory path or null if not set</returns>
    Task<string?> GetLastUsedOutputDirectoryAsync();
    
    /// <summary>
    /// Sets the last used directory for output folder selection
    /// </summary>
    /// <param name="directoryPath">Directory path to save</param>
    Task SetLastUsedOutputDirectoryAsync(string directoryPath);
    
    /// <summary>
    /// Gets the last used directory for existing project folder selection
    /// </summary>
    /// <returns>Directory path or null if not set</returns>
    Task<string?> GetLastUsedProjectDirectoryAsync();
    
    /// <summary>
    /// Sets the last used directory for existing project folder selection
    /// </summary>
    /// <param name="directoryPath">Directory path to save</param>
    Task SetLastUsedProjectDirectoryAsync(string directoryPath);
}