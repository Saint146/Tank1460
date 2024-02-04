using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Tank1460.SaveLoad.Settings;

namespace Tank1460.SaveLoad;

internal class SaveLoadManager
{
    private readonly string _rootSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tank1460");
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions DefaultOptions = new (){ WriteIndented = true };

    public void SaveSettings(UserSettings userSettings)
    {
        Save(userSettings, SettingsFileName);
    }

    public UserSettings LoadSettings()
    {
        var settings = Load<UserSettings>(SettingsFileName);
        return settings;
    }

    protected void Save<T>(T saveData, string fileName)
    {
        var filePath = Path.Combine(_rootSavePath, fileName);

        if (File.Exists(filePath))
            File.Move(filePath, filePath + ".bak", true);

        Directory.CreateDirectory(_rootSavePath);

        var dataSerialized = JsonSerializer.Serialize(saveData, DefaultOptions);
        File.WriteAllText(filePath, dataSerialized);
    }

    protected T Load<T>(string fileName)
    {
        var filePath = Path.Combine(_rootSavePath, fileName);

        if (!File.Exists(filePath))
            return default;

        try
        {
            var dataSerialized = File.ReadAllText(filePath);
            var saveData = JsonSerializer.Deserialize<T>(dataSerialized);
            return saveData;
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
            return default;
        }
    }
}