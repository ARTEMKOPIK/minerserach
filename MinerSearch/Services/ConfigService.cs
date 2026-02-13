using MSearch.Models;
using Newtonsoft.Json;
using System.IO;

namespace MSearch.Services;

public class ConfigService : IConfigService
{
    private readonly string _configPath;
    private AppSettings _settings = new();
    private Dictionary<string, object> _customValues = new();

    public AppSettings Settings => _settings;

    public ConfigService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinerSearch");
        
        Directory.CreateDirectory(appDataPath);
        _configPath = Path.Combine(appDataPath, "config.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var data = JsonConvert.DeserializeObject<ConfigData>(json);
                
                if (data != null)
                {
                    _settings = data.Settings ?? new AppSettings();
                    _customValues = data.CustomValues ?? new Dictionary<string, object>();
                }
            }
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Failed to load config");
            _settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var data = new ConfigData
            {
                Settings = _settings,
                CustomValues = _customValues
            };

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Failed to save config");
        }
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (_customValues.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;
                
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value)) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        _customValues[key] = value!;
        Save();
    }

    private class ConfigData
    {
        public AppSettings? Settings { get; set; }
        public Dictionary<string, object>? CustomValues { get; set; }
    }
}
