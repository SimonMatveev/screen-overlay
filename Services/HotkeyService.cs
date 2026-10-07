using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenOverlayApp.Services
{
    public class HotkeyService
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HotkeyId = 9000;
        private const int WmHotkey = 0x0312;

        private IntPtr _handle;
        private bool _hookAdded;
        private bool _isRegistered;
        private string _currentHotkey = "";
        private Action? _onHotkey;

        public void Register(Window window, string hotkey, Action onHotkey)
        {
            var helper = new WindowInteropHelper(window);
            _handle = helper.EnsureHandle();
            _onHotkey = onHotkey;
            _currentHotkey = hotkey;

            if (!_hookAdded)
            {
                var source = HwndSource.FromHwnd(_handle);
                source?.AddHook(WndProc);
                _hookAdded = true;
            }

            ApplyRegistration();
        }

        public void UpdateHotkey(Window window, string hotkey, Action onHotkey)
        {
            _onHotkey = onHotkey;
            _currentHotkey = hotkey;

            if (_handle == IntPtr.Zero)
            {
                Register(window, hotkey, onHotkey);
                return;
            }

            ApplyRegistration();
        }

        /// <summary>
        /// Temporarily release the OS hotkey so the same combo can be typed into the capture field.
        /// </summary>
        public void Suspend()
        {
            UnregisterCurrent();
        }

        public void Resume()
        {
            ApplyRegistration();
        }

        private void ApplyRegistration()
        {
            UnregisterCurrent();

            if (_handle == IntPtr.Zero || string.IsNullOrWhiteSpace(_currentHotkey))
                return;

            var (modifiers, key) = HotkeyParser.Parse(_currentHotkey);
            if (key == 0)
                return;

            _isRegistered = RegisterHotKey(_handle, HotkeyId, modifiers, key);
        }

        private void UnregisterCurrent()
        {
            if (_handle == IntPtr.Zero || !_isRegistered)
                return;

            UnregisterHotKey(_handle, HotkeyId);
            _isRegistered = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                _onHotkey?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }
    }
}
