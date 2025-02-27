using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EasyWorkspace
{
    public static class EWSettings
    {
        private static string _filePath = Application.persistentDataPath + "/EasyWorkspace/" + "Settings";
        private static Dictionary<string, string> _cachedSettings = new Dictionary<string, string>();

        public static bool SettingsIsOpen { get; set; }
        public static EWAssetActionType AssetActionLmb { get => ReadEnum(nameof(AssetActionLmb), EWAssetActionType.Open); set => WriteEnum(nameof(AssetActionLmb), value); }
        public static EWAssetActionType AssetActionRmb { get => ReadEnum(nameof(AssetActionRmb), EWAssetActionType.Select); set => WriteEnum(nameof(AssetActionRmb), value); }
        public static EWAssetActionType AssetActionMmb { get => ReadEnum(nameof(AssetActionMmb), EWAssetActionType.Show); set => WriteEnum(nameof(AssetActionMmb), value); }
        public static string LastBackupTime { get => Read(nameof(LastBackupTime), "-"); set => Write(nameof(LastBackupTime), value); }
        public static bool Snapping { get => Read(nameof(Snapping), false); set => Write(nameof(Snapping), value); }
        public static bool ZoomScrolling { get => Read(nameof(ZoomScrolling), true); set => Write(nameof(ZoomScrolling), value); }
        public static bool ZoomFit { get => Read(nameof(ZoomFit), true); set => Write(nameof(ZoomFit), value); }

        static EWSettings()
        {
            LoadSettings();
        }
    
        private static void LoadSettings()
        {
            if (!File.Exists(_filePath))
                return;
        
            try
            {
                string[] lines = File.ReadAllLines(_filePath);
                foreach (string line in lines)
                {
                    string[] splitLine = line.Split('=');
                    if (splitLine.Length == 2)
                    {
                        _cachedSettings[splitLine[0]] = splitLine[1];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading settings from file: {e.Message}");
            }
        }

        private static void Write<T>(string key, T value)
        {
            try
            {
                _cachedSettings[key] = value.ToString();
                using StreamWriter writer = new StreamWriter(_filePath, false);
                foreach (KeyValuePair<string, string> setting in _cachedSettings)
                {
                    writer.WriteLine($"{setting.Key}={setting.Value}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing settings to file: {e.Message}");
            }
        }

        private static T Read<T>(string key, T defaultValue)
        {
            if (_cachedSettings.TryGetValue(key, out string value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error converting value: {e.Message}");
                }
            }

            return defaultValue;
        }
    
        private static void WriteEnum<T>(string key, T value) where T : Enum
        {
            Write(key, (int) (object) value);
        }
    
        private static T ReadEnum<T>(string key, T defaultValue) where T : Enum
        {
            return (T) Enum.ToObject(typeof(T), Read(key, (int) (object) defaultValue));
        }
    }
}
