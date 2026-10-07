using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using ScreenOverlayApp.Models;
using ScreenOverlayApp.Services;
using Forms = System.Windows.Forms;
using WpfApp = System.Windows.Application;

namespace ScreenOverlayApp
{
    public partial class App : WpfApp
    {
        private Mutex? instanceMutex;
        private bool ownsMutex;
        private Forms.NotifyIcon? trayIcon;
        private MainWindow? mainWindow;
        private bool isExitRequested;

        private readonly SettingsService settingsService = new();
        private readonly AutostartService autostartService = new();
        private readonly OverlayService overlayService = new();
        private AppSettings settings = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            instanceMutex = new Mutex(true, "ScreenOverlayApp.SingleInstance", out ownsMutex);
            if (!ownsMutex)
            {
                instanceMutex.Dispose();
                instanceMutex = null;
                Shutdown();
                return;
            }

            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            settings = settingsService.Load();

            // Keep the Run key in sync so older installs also get --minimized.
            if (settings.AutoStart)
                autostartService.SetEnabled(true);

            var startMinimized = e.Args.Any(arg =>
                string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase)
            );

            mainWindow = new MainWindow(settings, overlayService);
            mainWindow.Closing += OnMainWindowClosing;

            InitTrayIcon();

            if (startMinimized)
            {
                // Show once so Loaded/hotkeys initialize, then hide to tray without a flash.
                mainWindow.ShowInTaskbar = false;
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.Show();
                mainWindow.Hide();
            }
            else
            {
                mainWindow.Show();
            }
        }

        public void PersistSettings(AppSettings updated)
        {
            settings = updated;
            settingsService.Save(settings);
            autostartService.SetEnabled(settings.AutoStart);
        }

        private void InitTrayIcon()
        {
            if (trayIcon != null)
                return;

            var icon =
                System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                ?? SystemIcons.Application;

            trayIcon = new Forms.NotifyIcon
            {
                Icon = icon,
                Text = "Screen Overlay",
                Visible = true,
            };

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => ShowMainWindow());
            menu.Items.Add(
                "Toggle overlay",
                null,
                (_, _) =>
                {
                    Dispatcher.Invoke(() => overlayService.Toggle(settings));
                }
            );
            menu.Items.Add("Exit", null, (_, _) => ExitApp());

            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        }

        private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isExitRequested)
                return;

            e.Cancel = true;
            mainWindow?.Hide();
        }

        private void ShowMainWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (mainWindow == null)
                    return;

                mainWindow.ShowInTaskbar = true;
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            });
        }

        public void ExitApp()
        {
            isExitRequested = true;

            Dispatcher.Invoke(() =>
            {
                DisposeTrayIcon();
                mainWindow?.Close();
                Shutdown();
            });
        }

        private void DisposeTrayIcon()
        {
            if (trayIcon == null)
                return;

            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayIcon = null;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DisposeTrayIcon();

            if (ownsMutex && instanceMutex != null)
            {
                instanceMutex.ReleaseMutex();
                instanceMutex.Dispose();
                instanceMutex = null;
            }

            base.OnExit(e);
        }
    }
}
