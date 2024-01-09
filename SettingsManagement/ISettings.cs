using System.ComponentModel;
using System.Text.Json;

namespace SettingsManagement;

[AttributeUsage(AttributeTargets.Field)]
public class SaveOnChangeAttribute : Attribute
{

}

public interface ISettings : INotifyPropertyChanged
{
}

public class SettingsManager<T> where T : ISettings, new()
{
    private const string SettingsFileName = "settings.json";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() {WriteIndented = true, AllowTrailingCommas = true};
    private readonly string _settingsFilePath;

    private T _settings;

    public SettingsManager(string appFolderPath)
    {
        _settingsFilePath = Path.Combine(appFolderPath, SettingsFileName);
    }

    public T LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            var json = File.ReadAllText(_settingsFilePath);
            _settings = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);

            if (_settings is null)
                throw new NotSupportedException($"Could not parse {_settingsFilePath} as {typeof(T).Name}");
        }
        else
        {
            _settings = new T();
            Save();
        }

        _settings.PropertyChanged += (_, _) => Save();
        
        return _settings;
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_settings, _jsonSerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }
}