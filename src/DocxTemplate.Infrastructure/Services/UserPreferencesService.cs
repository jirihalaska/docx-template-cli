using System.Text.Json;
using DocxTemplate.Core.Models;
using DocxTemplate.Core.Services;
using Microsoft.Extensions.Logging;

namespace DocxTemplate.Infrastructure.Services;

/// <summary>
/// Service for managing user preferences stored in local application data folder
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly string _preferencesFilePath;
    private UserPreferences? _cachedPreferences;
    private readonly object _lockObject = new();

    public UserPreferencesService(ILogger<UserPreferencesService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        try
        {
            // Get platform-specific app data directory
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolder, "DocxTemplate");
            
            // Ensure directory exists
            Directory.CreateDirectory(appFolder);
            
            _preferencesFilePath = Path.Combine(appFolder, "preferences.json");
            
            _logger.LogDebug("User preferences will be stored at: {PreferencesFilePath}", _preferencesFilePath);
        }
        catch (Exception ex)
        {
            // Fallback to a safe location if we can't create the preferred directory
            _logger.LogWarning(ex, "Failed to create preferences directory in LocalApplicationData, falling back to temp directory");
            
            var tempFolder = Path.GetTempPath();
            var fallbackFolder = Path.Combine(tempFolder, "DocxTemplate");
            
            try
            {
                Directory.CreateDirectory(fallbackFolder);
                _preferencesFilePath = Path.Combine(fallbackFolder, "preferences.json");
                _logger.LogInformation("User preferences will be stored at fallback location: {PreferencesFilePath}", _preferencesFilePath);
            }
            catch (Exception fallbackEx)
            {
                // Last resort: disable preferences by using an invalid path that will be handled gracefully
                _logger.LogError(fallbackEx, "Failed to create fallback preferences directory, preferences will be disabled");
                _preferencesFilePath = string.Empty; // This will cause all operations to use defaults
            }
        }
    }

    public async Task<string?> GetLastUsedImageDirectoryAsync()
    {
        var preferences = await LoadPreferencesAsync();
        return preferences.LastUsedImageDirectory;
    }

    public async Task SetLastUsedImageDirectoryAsync(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        
        var preferences = await LoadPreferencesAsync();
        preferences.LastUsedImageDirectory = directoryPath;
        await SavePreferencesAsync(preferences);
        
        _logger.LogDebug("Saved last used image directory: {DirectoryPath}", directoryPath);
    }

    public async Task<string?> GetLastUsedOutputDirectoryAsync()
    {
        var preferences = await LoadPreferencesAsync();
        return preferences.LastUsedOutputDirectory;
    }

    public async Task SetLastUsedOutputDirectoryAsync(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        
        var preferences = await LoadPreferencesAsync();
        preferences.LastUsedOutputDirectory = directoryPath;
        await SavePreferencesAsync(preferences);
        
        _logger.LogDebug("Saved last used output directory: {DirectoryPath}", directoryPath);
    }

    public async Task<string?> GetLastUsedProjectDirectoryAsync()
    {
        var preferences = await LoadPreferencesAsync();
        return preferences.LastUsedProjectDirectory;
    }

    public async Task SetLastUsedProjectDirectoryAsync(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        
        var preferences = await LoadPreferencesAsync();
        preferences.LastUsedProjectDirectory = directoryPath;
        await SavePreferencesAsync(preferences);
        
        _logger.LogDebug("Saved last used project directory: {DirectoryPath}", directoryPath);
    }

    private async Task<UserPreferences> LoadPreferencesAsync()
    {
        lock (_lockObject)
        {
            if (_cachedPreferences != null)
            {
                return _cachedPreferences;
            }
        }

        // If preferences are disabled (empty path), always return defaults
        if (string.IsNullOrEmpty(_preferencesFilePath))
        {
            _logger.LogDebug("Preferences are disabled, using defaults");
            var defaultPreferences = new UserPreferences();
            
            lock (_lockObject)
            {
                _cachedPreferences = defaultPreferences;
            }
            
            return defaultPreferences;
        }

        try
        {
            if (!File.Exists(_preferencesFilePath))
            {
                _logger.LogDebug("Preferences file does not exist, creating default preferences");
                var defaultPreferences = new UserPreferences();
                
                lock (_lockObject)
                {
                    _cachedPreferences = defaultPreferences;
                }
                
                return defaultPreferences;
            }

            var jsonContent = await File.ReadAllTextAsync(_preferencesFilePath);
            var preferences = JsonSerializer.Deserialize<UserPreferences>(jsonContent) ?? new UserPreferences();
            
            lock (_lockObject)
            {
                _cachedPreferences = preferences;
            }
            
            _logger.LogDebug("Loaded user preferences from {PreferencesFilePath}", _preferencesFilePath);
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user preferences from {PreferencesFilePath}, using defaults", _preferencesFilePath);
            
            var defaultPreferences = new UserPreferences();
            lock (_lockObject)
            {
                _cachedPreferences = defaultPreferences;
            }
            
            return defaultPreferences;
        }
    }

    private async Task SavePreferencesAsync(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        // If preferences are disabled (empty path), just update cache and return
        if (string.IsNullOrEmpty(_preferencesFilePath))
        {
            _logger.LogDebug("Preferences are disabled, updating cache only");
            lock (_lockObject)
            {
                _cachedPreferences = preferences;
            }
            return;
        }

        try
        {
            var jsonContent = JsonSerializer.Serialize(preferences, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_preferencesFilePath, jsonContent);
            
            lock (_lockObject)
            {
                _cachedPreferences = preferences;
            }
            
            _logger.LogDebug("Saved user preferences to {PreferencesFilePath}", _preferencesFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user preferences to {PreferencesFilePath}", _preferencesFilePath);
            
            // Still update the cache so the preferences work for this session
            lock (_lockObject)
            {
                _cachedPreferences = preferences;
            }
        }
    }
}