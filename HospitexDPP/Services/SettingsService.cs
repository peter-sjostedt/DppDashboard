using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HospitexDPP.Services
{
    public class SavedKey
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class WindowSettings
    {
        [JsonPropertyName("width")]
        public double Width { get; set; } = 1024;

        [JsonPropertyName("height")]
        public double Height { get; set; } = 768;

        [JsonPropertyName("left")]
        public double Left { get; set; } = double.NaN;

        [JsonPropertyName("top")]
        public double Top { get; set; } = double.NaN;

        [JsonPropertyName("maximized")]
        public bool Maximized { get; set; }
    }

    public class AppSettings
    {
        [JsonPropertyName("keys")]
        public List<SavedKey> Keys { get; set; } = new();

        [JsonPropertyName("language")]
        public string Language { get; set; } = "sv";

        [JsonPropertyName("window")]
        public WindowSettings? Window { get; set; }

        [JsonPropertyName("filters")]
        public Dictionary<string, string> Filters { get; set; } = new();
    }

    public static class SettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HospitexDPP");

        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                           ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static void SaveFilter(string key, string value)
        {
            var settings = Load();
            settings.Filters[key] = value;
            Save(settings);
        }

        public static string? LoadFilter(string key)
        {
            var settings = Load();
            return settings.Filters.TryGetValue(key, out var value) ? value : null;
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(SettingsPath))
                    File.Delete(SettingsPath);
            }
            catch { }
        }
    }
}
