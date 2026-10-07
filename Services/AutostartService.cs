using System.Diagnostics;
using Microsoft.Win32;

namespace ScreenOverlayApp.Services
{
    public class AutostartService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "ScreenOverlayApp";

        public bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppName) is string;
        }

        public void SetEnabled(bool enabled)
        {
            using var key =
                Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (enabled)
            {
                var exePath =
                    Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(exePath))
                    return;

                key.SetValue(AppName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
    }
}
