using System.Collections.Generic;
using System.Linq;
using ScreenOverlayApp.Models;
using Screen = System.Windows.Forms.Screen;

namespace ScreenOverlayApp.Services
{
    public class OverlayService
    {
        private List<OverlayWindow> overlays = new();

        public bool IsActive => overlays.Any();

        public void Show(AppSettings settings)
        {
            Hide();

            foreach (var screen in Screen.AllScreens)
            {
                if (!settings.EnabledMonitors.Contains(screen.DeviceName))
                    continue;

                var overlay = new OverlayWindow(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Bounds.Width,
                    screen.Bounds.Height
                );

                overlay.Show();
                overlays.Add(overlay);
            }
        }

        public void Hide()
        {
            foreach (var o in overlays)
                o.Close();

            overlays.Clear();
        }

        public void Toggle(AppSettings settings)
        {
            if (IsActive)
                Hide();
            else
                Show(settings);
        }
    }
}
