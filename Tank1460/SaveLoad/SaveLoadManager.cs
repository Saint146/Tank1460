﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Tank1460.SaveLoad;

internal class SaveLoadManager
{
    readonly string rootSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tank1460");
    private const string settingsFileName = "settings.json";

    public void SaveSettings(SettingsData settings)
    {
        Save(settings, settingsFileName);
    }

    public SettingsData LoadSettings()
    {
        var settings = Load<SettingsData>(settingsFileName);
        return settings;
    }

    protected void Save<T>(T saveData, string fileName)
    {
        var filePath = Path.Combine(rootSavePath, fileName);

        if (File.Exists(filePath))
            File.Move(filePath, filePath + ".bak", true);

        Directory.CreateDirectory(rootSavePath);

        var dataSerialized = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, dataSerialized);
    }

    protected T Load<T>(string fileName)
    {
        var filePath = Path.Combine(rootSavePath, fileName);

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