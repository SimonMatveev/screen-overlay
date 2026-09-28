using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace ScreenOverlayApp.Services
{
    public class HotkeyService
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000; 
        private IntPtr _handle;

        private bool _hookAdded = false;

        public void Register(Window window, string hotkey, Action onHotkey)
        {
            var helper = new WindowInteropHelper(window);
            _handle = helper.Handle;

            var (modifiers, key) = HotkeyParser.Parse(hotkey);

            var source = HwndSource.FromHwnd(_handle);
            {
                if (!_hookAdded)
                {
                    source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        if (msg == 0x0312)
                     {
                         onHotkey();
                         handled = true;
                     }
                     return IntPtr.Zero;
                     });
                    _hookAdded = true;

                }
                RegisterHotKey(_handle, HOTKEY_ID, modifiers, key);
            }
        }

        public void UpdateHotkey(Window window, string hotkey, Action onHotkey)
        {
            if (_handle != IntPtr.Zero)
            {
                UnregisterHotKey(_handle, HOTKEY_ID);
            }

            Register(window, hotkey, onHotkey);
        }
    }
}