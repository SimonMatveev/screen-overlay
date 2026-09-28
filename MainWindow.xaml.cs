using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScreenOverlayApp.Models;
using ScreenOverlayApp.Services;
using Controls = System.Windows.Controls;
using Screen = System.Windows.Forms.Screen;

namespace ScreenOverlayApp
{
    public partial class MainWindow : Window
    {
        private const string HotkeyCapturePrompt = "Нажмите сочетание...";

        private AppSettings settings;
        private readonly OverlayService overlayService;
        private readonly HotkeyService hotkeyService = new();
        private bool isLoading = true;
        private string hotkeyBeforeEdit = "";
        private bool isCapturingHotkey;

        public MainWindow(AppSettings settings, OverlayService overlayService)
        {
            InitializeComponent();

            this.settings = settings;
            this.overlayService = overlayService;

            HotkeyBox.Text = settings.Hotkey;
            AutoStartCheck.IsChecked = settings.AutoStart;

            LoadMonitors();
            isLoading = false;

            Loaded += OnLoaded;
        }

        private App AppInstance => (App)System.Windows.Application.Current;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            hotkeyService.Register(
                this,
                settings.Hotkey,
                () =>
                {
                    overlayService.Toggle(settings);
                }
            );
        }

        private void LoadMonitors()
        {
            foreach (var screen in Screen.AllScreens)
            {
                var name = MonitorInfo.GetFriendlyName(screen);
                var resolution = $"{screen.Bounds.Width}x{screen.Bounds.Height}";

                var checkbox = new Controls.CheckBox
                {
                    Content = $"{name} ({resolution})",
                    Tag = screen.DeviceName,
                    IsChecked = settings.EnabledMonitors.Contains(screen.DeviceName),
                };

                checkbox.Checked += OnSettingChanged;
                checkbox.Unchecked += OnSettingChanged;

                MonitorsList.Items.Add(checkbox);
            }
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (isLoading)
                return;

            SaveSettings(updateHotkey: false);
        }

        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            CollectMonitorSettings();
            overlayService.Toggle(settings);
        }

        private void CollectMonitorSettings()
        {
            settings.EnabledMonitors = MonitorsList
                .Items.Cast<Controls.CheckBox>()
                .Where(c => c.IsChecked == true)
                .Select(c => (string)c.Tag!)
                .ToList();
        }

        private void SaveSettings(bool updateHotkey)
        {
            CollectMonitorSettings();

            if (HotkeyBox.Text != HotkeyCapturePrompt && !string.IsNullOrWhiteSpace(HotkeyBox.Text))
                settings.Hotkey = HotkeyBox.Text;

            settings.AutoStart = AutoStartCheck.IsChecked == true;

            AppInstance.PersistSettings(settings);

            if (updateHotkey)
            {
                hotkeyService.UpdateHotkey(
                    this,
                    settings.Hotkey,
                    () =>
                    {
                        overlayService.Toggle(settings);
                    }
                );
            }
        }

        private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            isCapturingHotkey = true;
            hotkeyBeforeEdit =
                HotkeyBox.Text == HotkeyCapturePrompt ? settings.Hotkey : HotkeyBox.Text;

            HotkeyBox.Text = HotkeyCapturePrompt;
            HotkeyBox.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 249, 196)
            );
            HotkeyBox.BorderBrush = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(218, 165, 32)
            );
            HotkeyBox.BorderThickness = new Thickness(2);
            HotkeyBox.Foreground = System.Windows.Media.Brushes.DimGray;
            HotkeyHint.Text = "Ожидание нажатия клавиш...";
            HotkeyHint.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(180, 120, 0)
            );
            HotkeyHint.FontWeight = FontWeights.SemiBold;
        }

        private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isCapturingHotkey)
                return;

            isCapturingHotkey = false;

            if (string.IsNullOrWhiteSpace(HotkeyBox.Text) || HotkeyBox.Text == HotkeyCapturePrompt)
                HotkeyBox.Text = hotkeyBeforeEdit;

            ResetHotkeyBoxAppearance();
        }

        private void ResetHotkeyBoxAppearance()
        {
            HotkeyBox.ClearValue(Controls.Control.BackgroundProperty);
            HotkeyBox.ClearValue(Controls.Control.BorderBrushProperty);
            HotkeyBox.ClearValue(Controls.Control.BorderThicknessProperty);
            HotkeyBox.ClearValue(Controls.Control.ForegroundProperty);
            HotkeyHint.Text = "Кликните и нажмите сочетание клавиш";
            HotkeyHint.Foreground = System.Windows.Media.Brushes.Gray;
            HotkeyHint.FontWeight = FontWeights.Normal;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            var modifiers = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.Escape)
            {
                HotkeyBox.Text = hotkeyBeforeEdit;
                Keyboard.ClearFocus();
                return;
            }

            string result = "";

            if (modifiers.HasFlag(ModifierKeys.Control))
                result += "Ctrl+";

            if (modifiers.HasFlag(ModifierKeys.Alt))
                result += "Alt+";

            if (modifiers.HasFlag(ModifierKeys.Shift))
                result += "Shift+";

            if (
                key == Key.LeftCtrl
                || key == Key.RightCtrl
                || key == Key.LeftAlt
                || key == Key.RightAlt
                || key == Key.LeftShift
                || key == Key.RightShift
            )
            {
                return;
            }

            result += key.ToString();

            HotkeyBox.Text = result;
            HotkeyBox.Foreground = System.Windows.Media.Brushes.Black;
            SaveSettings(updateHotkey: true);
            Keyboard.ClearFocus();
        }
    }
}
