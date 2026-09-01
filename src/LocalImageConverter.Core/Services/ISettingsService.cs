using LocalImageConverter.Core.Models;

namespace LocalImageConverter.Core.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    void ResetToDefaults();
}
