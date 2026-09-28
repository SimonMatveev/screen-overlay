using System.Collections.Generic;
using System.Linq;
using ScreenOverlayApp.Models;
using Screen = System.Windows.Forms.Screen;

namespace ScreenOverlayApp.Services
{
    public class OverlayService
    {
        private readonly List<OverlayWindow> overlays = new();
        private bool isActive;

        public bool IsActive => isActive;

        public void Show(AppSettings settings)
        {
            isActive = true;
            Rebuild(settings);
        }

        public void Hide()
        {
            isActive = false;
            CloseAll();
        }

        public void Toggle(AppSettings settings)
        {
            if (isActive)
                Hide();
            else
                Show(settings);
        }

        /// <summary>
        /// Rebuilds visible overlays from current settings while the overlay stays armed.
        /// </summary>
        public void Refresh(AppSettings settings)
        {
            if (!isActive)
                return;

            Rebuild(settings);
        }

        private void Rebuild(AppSettings settings)
        {
            CloseAll();

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

        private void CloseAll()
        {
            foreach (var overlay in overlays.ToList())
            {
                try
                {
                    overlay.Close();
                }
                catch
                {
                    // Ignore already-closed windows.
                }
            }

            overlays.Clear();
        }
    }
}
