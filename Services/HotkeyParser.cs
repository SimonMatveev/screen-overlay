using System;
using System.Windows.Input;

namespace ScreenOverlayApp.Services
{
    public static class HotkeyParser
    {
        public static (uint modifiers, uint key) Parse(string hotkey)
        {
            uint modifiers = 0;
            uint key = 0;

            var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var p = part.Trim().ToLower();

                switch (p)
                {
                    case "ctrl":
                        modifiers |= 0x0002;
                        break;
                    case "alt":
                        modifiers |= 0x0001;
                        break;
                    case "shift":
                        modifiers |= 0x0004;
                        break;
                    default:
                        key = (uint)KeyInterop.VirtualKeyFromKey(
                            (Key)Enum.Parse(typeof(Key), part, true)
                        );
                        break;
                }
            }

            return (modifiers, key);
        }
    }
}