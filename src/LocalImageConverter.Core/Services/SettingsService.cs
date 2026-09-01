using System.Text.Json;
using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILoggerService? _logger;
    private AppSettings _currentSettings;

    public AppSettings CurrentSettings => _currentSettings;

    public SettingsService(ILoggerService? logger = null)
    {
        _logger = logger;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "LocalImageConverter");
        _settingsFilePath = Path.Combine(dir, "settings.json");

        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to create settings directory", ex);
        }

        _currentSettings = LoadSettings();
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loaded != null)
                {
                    _currentSettings = loaded;
                    return _currentSettings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to read settings file, returning defaults", ex);
        }

        _currentSettings = new AppSettings();
        return _currentSettings;
    }

    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings ?? new AppSettings();
        try
        {
            var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to save settings file", ex);
        }
    }

    public void ResetToDefaults()
    {
        _currentSettings = new AppSettings();
        SaveSettings(_currentSettings);
    }
}
