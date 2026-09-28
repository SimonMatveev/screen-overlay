using System.IO;
using System.Text.Json;
using ScreenOverlayApp.Models;

namespace ScreenOverlayApp.Services
{
    public class SettingsService
    {
        private readonly string path;

        public SettingsService()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScreenOverlayApp"
            );

            Directory.CreateDirectory(appData);
            path = Path.Combine(appData, "settings.json");
        }

        public AppSettings Load()
        {
            var sourcePath = File.Exists(path) ? path : GetLegacyPath();

            if (sourcePath == null || !File.Exists(sourcePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(sourcePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions { WriteIndented = true }
            );

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, json);
        }

        private static string? GetLegacyPath()
        {
            var baseDir = AppContext.BaseDirectory;
            if (string.IsNullOrWhiteSpace(baseDir))
                return null;

            return Path.Combine(baseDir, "settings.json");
        }
    }
}
