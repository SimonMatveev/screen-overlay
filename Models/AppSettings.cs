using System.Collections.Generic;

namespace ScreenOverlayApp.Models
{
    public class AppSettings
    {
        public List<string> EnabledMonitors { get; set; } = new();
        public string Hotkey { get; set; } = "Ctrl+Alt+B";
        public bool AutoStart { get; set; }
    }
}
