using System.Runtime.InteropServices;
using Screen = System.Windows.Forms.Screen;

namespace ScreenOverlayApp.Services
{
    internal static class MonitorInfo
    {
        private const uint QdcOnlyActivePaths = 2;
        private const int ErrorSuccess = 0;
        private const int DisplayConfigDeviceInfoGetSourceName = 1;
        private const int DisplayConfigDeviceInfoGetTargetName = 2;

        public static string GetFriendlyName(Screen screen)
        {
            try
            {
                var map = GetFriendlyNameMap();
                if (
                    map.TryGetValue(screen.DeviceName, out var name)
                    && !string.IsNullOrWhiteSpace(name)
                    && !IsGenericName(name)
                )
                {
                    return name;
                }
            }
            catch
            {
                // Fall through to device-name fallback.
            }

            return GetFallbackName(screen);
        }

        private static bool IsGenericName(string name) =>
            name.Contains("Generic PnP", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Generic Monitor", StringComparison.OrdinalIgnoreCase);

        private static string GetFallbackName(Screen screen)
        {
            var device = screen.DeviceName;
            var slash = device.LastIndexOf('\\');
            var shortName = slash >= 0 ? device[(slash + 1)..] : device;
            return screen.Primary ? $"{shortName} (основной)" : shortName;
        }

        private static Dictionary<string, string> GetFriendlyNameMap()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (
                GetDisplayConfigBufferSizes(QdcOnlyActivePaths, out var pathCount, out var modeCount)
                != ErrorSuccess
            )
            {
                return result;
            }

            var paths = new DisplayConfigPathInfo[pathCount];
            var modes = new DisplayConfigModeInfo[modeCount];

            if (
                QueryDisplayConfig(
                    QdcOnlyActivePaths,
                    ref pathCount,
                    paths,
                    ref modeCount,
                    modes,
                    IntPtr.Zero
                ) != ErrorSuccess
            )
            {
                return result;
            }

            foreach (var path in paths)
            {
                var sourceName = new DisplayConfigSourceDeviceName
                {
                    header = new DisplayConfigDeviceInfoHeader
                    {
                        type = DisplayConfigDeviceInfoGetSourceName,
                        size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                        adapterId = path.sourceInfo.adapterId,
                        id = path.sourceInfo.id,
                    },
                };

                var targetName = new DisplayConfigTargetDeviceName
                {
                    header = new DisplayConfigDeviceInfoHeader
                    {
                        type = DisplayConfigDeviceInfoGetTargetName,
                        size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>(),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id,
                    },
                };

                if (DisplayConfigGetDeviceInfoSource(ref sourceName) != ErrorSuccess)
                    continue;

                if (DisplayConfigGetDeviceInfoTarget(ref targetName) != ErrorSuccess)
                    continue;

                var gdiName = sourceName.viewGdiDeviceName;
                var friendly = targetName.monitorFriendlyDeviceName;

                if (!string.IsNullOrWhiteSpace(gdiName) && !string.IsNullOrWhiteSpace(friendly))
                    result[gdiName] = friendly.Trim();
            }

            return result;
        }

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(
            uint flags,
            out uint numPathArrayElements,
            out uint numModeInfoArrayElements
        );

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DisplayConfigPathInfo[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DisplayConfigModeInfo[] modeInfoArray,
            IntPtr currentTopologyId
        );

        [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
        private static extern int DisplayConfigGetDeviceInfoSource(
            ref DisplayConfigSourceDeviceName requestPacket
        );

        [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
        private static extern int DisplayConfigGetDeviceInfoTarget(
            ref DisplayConfigTargetDeviceName requestPacket
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct Luid
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DisplayConfigPathInfo
        {
            public DisplayConfigPathSourceInfo sourceInfo;
            public DisplayConfigPathTargetInfo targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DisplayConfigPathSourceInfo
        {
            public Luid adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DisplayConfigPathTargetInfo
        {
            public Luid adapterId;
            public uint id;
            public uint modeInfoIdx;
            public int outputTechnology;
            public uint rotation;
            public uint scaling;
            public DisplayConfigRational refreshRate;
            public int scanLineOrdering;
            public int targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DisplayConfigRational
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct DisplayConfigModeInfo
        {
            public uint infoType;
            public uint id;
            public Luid adapterId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DisplayConfigDeviceInfoHeader
        {
            public int type;
            public uint size;
            public Luid adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DisplayConfigSourceDeviceName
        {
            public DisplayConfigDeviceInfoHeader header;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string viewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DisplayConfigTargetDeviceName
        {
            public DisplayConfigDeviceInfoHeader header;
            public uint flags;
            public int outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }
    }
}
