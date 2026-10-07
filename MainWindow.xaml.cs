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
        private const string HotkeyCapturePrompt = "Press a key combination...";

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

            // Monitor checkboxes store DeviceName in Tag; AutoStart does not.
            if (!overlayService.IsActive || sender is not Controls.CheckBox { Tag: string })
                return;

            // Defer rebuild so we don't close/recreate windows inside the checkbox event.
            Dispatcher.BeginInvoke(() =>
            {
                overlayService.Refresh(settings);
                Activate();
            });
        }

        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            CollectMonitorSettings();
            overlayService.Toggle(settings);

            if (overlayService.IsActive)
                Activate();
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

        private void HotkeyBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Re-clicking an already-focused box does not raise GotFocus again.
            if (!isCapturingHotkey)
            {
                BeginHotkeyCapture();
                HotkeyBox.Focus();
                e.Handled = true;
            }
        }

        private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            BeginHotkeyCapture();
        }

        private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isCapturingHotkey)
                return;

            CancelHotkeyCapture();
        }

        private void BeginHotkeyCapture()
        {
            if (isCapturingHotkey)
                return;

            isCapturingHotkey = true;
            hotkeyBeforeEdit =
                HotkeyBox.Text == HotkeyCapturePrompt ? settings.Hotkey : HotkeyBox.Text;

            // Release OS hotkey so the current combo reaches the text box instead of toggling overlay.
            hotkeyService.Suspend();

            HotkeyBox.Text = HotkeyCapturePrompt;
            HotkeyBox.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 249, 196)
            );
            HotkeyBox.BorderBrush = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(218, 165, 32)
            );
            HotkeyBox.BorderThickness = new Thickness(2);
            HotkeyBox.Foreground = System.Windows.Media.Brushes.DimGray;
            HotkeyHint.Text = "Waiting for key press...";
            HotkeyHint.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(180, 120, 0)
            );
            HotkeyHint.FontWeight = FontWeights.SemiBold;
        }

        private void CancelHotkeyCapture()
        {
            isCapturingHotkey = false;
            HotkeyBox.Text = string.IsNullOrWhiteSpace(hotkeyBeforeEdit)
                ? settings.Hotkey
                : hotkeyBeforeEdit;
            ResetHotkeyBoxAppearance();
            hotkeyService.Resume();
        }

        private void CommitHotkeyCapture(string hotkey)
        {
            isCapturingHotkey = false;
            HotkeyBox.Text = hotkey;
            ResetHotkeyBoxAppearance();
            SaveSettings(updateHotkey: true);
            MoveFocusAwayFromHotkeyBox();
        }

        private void ResetHotkeyBoxAppearance()
        {
            HotkeyBox.ClearValue(Controls.Control.BackgroundProperty);
            HotkeyBox.ClearValue(Controls.Control.BorderBrushProperty);
            HotkeyBox.ClearValue(Controls.Control.BorderThicknessProperty);
            HotkeyBox.ClearValue(Controls.Control.ForegroundProperty);
            HotkeyHint.Text = "Click to select hotkey combination";
            HotkeyHint.Foreground = System.Windows.Media.Brushes.Gray;
            HotkeyHint.FontWeight = FontWeights.Normal;
        }

        private void MoveFocusAwayFromHotkeyBox()
        {
            FocusManager.SetFocusedElement(this, this);
            Keyboard.Focus(this);
        }

        private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!isCapturingHotkey)
                return;

            e.Handled = true;

            var modifiers = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.Escape)
            {
                CancelHotkeyCapture();
                MoveFocusAwayFromHotkeyBox();
                return;
            }

            if (
                key == Key.LeftCtrl
                || key == Key.RightCtrl
                || key == Key.LeftAlt
                || key == Key.RightAlt
                || key == Key.LeftShift
                || key == Key.RightShift
                || key == Key.LWin
                || key == Key.RWin
            )
            {
                return;
            }

            string result = "";

            if (modifiers.HasFlag(ModifierKeys.Control))
                result += "Ctrl+";

            if (modifiers.HasFlag(ModifierKeys.Alt))
                result += "Alt+";

            if (modifiers.HasFlag(ModifierKeys.Shift))
                result += "Shift+";

            result += key.ToString();

            CommitHotkeyCapture(result);
        }
    }
}
