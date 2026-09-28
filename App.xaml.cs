using System.Drawing;
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

            mainWindow = new MainWindow(settings, overlayService);
            mainWindow.Closing += OnMainWindowClosing;

            InitTrayIcon();
            mainWindow.Show();
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
            menu.Items.Add("Открыть", null, (_, _) => ShowMainWindow());
            menu.Items.Add(
                "Переключить оверлей",
                null,
                (_, _) =>
                {
                    Dispatcher.Invoke(() => overlayService.Toggle(settings));
                }
            );
            menu.Items.Add("Выход", null, (_, _) => ExitApp());

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
