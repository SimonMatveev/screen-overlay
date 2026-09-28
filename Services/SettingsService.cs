using System.IO;
using System.Text.Json;
using ScreenOverlayApp.Models;

namespace ScreenOverlayApp.Services
{
    public class SettingsService
    {
        private readonly string path = "settings.json";

        public AppSettings Load()
        {
            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }
    }
}