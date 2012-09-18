using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DREAMPioneer
{

    public class DisplayFinder
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }
        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x16,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        
        [DllImport("user32.dll")]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        public System.Windows.Point Find3Ms()
        {
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);
            uint dev = 0;  // device index
            int id = 1;        // monitor number, as used by Display Properties > Settings

            string DeviceID;
            Dictionary<DISPLAY_DEVICE, DEVMODE> dms = new Dictionary<DISPLAY_DEVICE, DEVMODE>();

            while (EnumDisplayDevices(null, dev, ref dd, 0))
            {
                DISPLAY_DEVICE ddMon = new DISPLAY_DEVICE();
                ddMon.cb = Marshal.SizeOf(ddMon);
                uint devMon = 0;

                while (EnumDisplayDevices(dd.DeviceName, devMon, ref ddMon, 0))
                {
                    string devicename = ddMon.DeviceName;
                    devicename = devicename.Substring(0, devicename.IndexOf("\\Monitor"));
                    Console.WriteLine("Key: {0}\nID: {1}\nName: {2}\nString: {3}", ddMon.DeviceKey, ddMon.DeviceID, ddMon.DeviceName, ddMon.DeviceString);
                    DEVMODE dm = new DEVMODE { dmDeviceName = new String(new char[32]), dmFormName = new String(new char[32]), };
                    dm.dmSize = (short)Marshal.SizeOf(dm);
                    if (EnumDisplaySettingsEx(devicename, -1, ref dm, 0))
                    {
                        dms.Add(ddMon, dm);
                    }
                    devMon++;
                }
                dev++;
            }

            if (dms.Count < 2)
                return new System.Windows.Point();
            List<DEVMODE> highests = new List<DEVMODE>();
            int highestdisp = -1;
            for (int i = 0; i < 10; i++)
                foreach (DISPLAY_DEVICE d in dms.Keys)
                {
                    if (d.DeviceName.Contains("\\\\.\\DISPLAY" + i))
                    {
                        if (i > highestdisp)
                        {
                            highests.Clear();
                            highestdisp = i;
                        }
                        highests.Add(dms[d]);
                    }
                }
            if (highests.Count != 1) throw new Exception("this function is brittle. woops.");
            return new System.Windows.Point((double)highests[0].dmPosition.x, (double)highests[0].dmPosition.y);
        }

        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettingsEx([MarshalAs(UnmanagedType.LPStr)]string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

        // size of a device name string
        private const int CCHDEVICENAME = 32;



        /// <summary>
        /// The struct that contains the display information
        /// </summary>
        public class DisplayInfo
        {
            public string Availability { get; set; }
            public string ScreenHeight { get; set; }
            public string ScreenWidth { get; set; }
            public RectStruct MonitorArea { get; set; }
            public RectStruct WorkArea { get; set; }
            public MonitorInfo originfo;
            public MonitorInfoEx originfoex;
        }

        /// <summary>
        /// Collection of display information
        /// </summary>
        public class DisplayInfoCollection : List<DisplayInfo>
        {
        }
        /// <summary>
        /// Returns the number of Displays using the Win32 functions
        /// </summary>
        /// <returns>collection of Display Info</returns>
        public DisplayInfoCollection GetDisplays()
        {
            DisplayInfoCollection col = new DisplayInfoCollection();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
                {
                    MonitorInfo mi = new MonitorInfo();
                    mi.Size = /*(uint)*/Marshal.SizeOf(mi);
                    bool success = GetMonitorInfo(hMonitor, ref mi);
                    if (success)
                    {
                        DisplayInfo di = new DisplayInfo();
                        di.originfo = mi;
                        di.ScreenWidth = (mi.Monitor.Right - mi.Monitor.Left).ToString();
                        di.ScreenHeight = (mi.Monitor.Bottom - mi.Monitor.Top).ToString();
                        di.MonitorArea = mi.Monitor;
                        di.WorkArea = mi.WorkArea;
                        di.Availability = mi.Flags.ToString();
                        col.Add(di);
                    }
                    return true;
                }, IntPtr.Zero);
            return col;
        }

        #region enums
        enum DISP_CHANGE : int
        {
            Successful = 0,
            Restart = 1,
            Failed = -1,
            BadMode = -2,
            NotUpdated = -3,
            BadFlags = -4,
            BadParam = -5,
            BadDualView = -6
        }

        [Flags()]
        public enum DM : int
        {
            Orientation = 0x1,
            PaperSize = 0x2,
            PaperLength = 0x4,
            PaperWidth = 0x8,
            Scale = 0x10,
            Position = 0x20,
            NUP = 0x40,
            DisplayOrientation = 0x80,
            Copies = 0x100,
            DefaultSource = 0x200,
            PrintQuality = 0x400,
            Color = 0x800,
            Duplex = 0x1000,
            YResolution = 0x2000,
            TTOption = 0x4000,
            Collate = 0x8000,
            FormName = 0x10000,
            LogPixels = 0x20000,
            BitsPerPixel = 0x40000,
            PelsWidth = 0x80000,
            PelsHeight = 0x100000,
            DisplayFlags = 0x200000,
            DisplayFrequency = 0x400000,
            ICMMethod = 0x800000,
            ICMIntent = 0x1000000,
            MediaType = 0x2000000,
            DitherType = 0x4000000,
            PanningWidth = 0x8000000,
            PanningHeight = 0x10000000,
            DisplayFixedOutput = 0x20000000
        }
        #endregion

        #region structs
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            public const int CCHDEVICENAME = 32;
            public const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            [System.Runtime.InteropServices.FieldOffset(0)]
            public string dmDeviceName;
            [System.Runtime.InteropServices.FieldOffset(32)]
            public Int16 dmSpecVersion;
            [System.Runtime.InteropServices.FieldOffset(34)]
            public Int16 dmDriverVersion;
            [System.Runtime.InteropServices.FieldOffset(36)]
            public Int16 dmSize;
            [System.Runtime.InteropServices.FieldOffset(38)]
            public Int16 dmDriverExtra;
            [System.Runtime.InteropServices.FieldOffset(40)]
            public DM dmFields;
            /*
                        [System.Runtime.InteropServices.FieldOffset(44)]
                        Int16 dmOrientation;
                        [System.Runtime.InteropServices.FieldOffset(46)]
                        Int16 dmPaperSize;
                        [System.Runtime.InteropServices.FieldOffset(48)]
                        Int16 dmPaperLength;
                        [System.Runtime.InteropServices.FieldOffset(50)]
                        Int16 dmPaperWidth;
                        [System.Runtime.InteropServices.FieldOffset(52)]
                        Int16 dmScale;
                        [System.Runtime.InteropServices.FieldOffset(54)]
                        Int16 dmCopies;
                        [System.Runtime.InteropServices.FieldOffset(56)]
                        Int16 dmDefaultSource;
                        [System.Runtime.InteropServices.FieldOffset(58)]
                        Int16 dmPrintQuality;*/

            [System.Runtime.InteropServices.FieldOffset(44)]
            public POINTL dmPosition;
            [System.Runtime.InteropServices.FieldOffset(52)]
            public Int32 dmDisplayOrientation;
            [System.Runtime.InteropServices.FieldOffset(56)]
            public Int32 dmDisplayFixedOutput;

            [System.Runtime.InteropServices.FieldOffset(60)]
            public short dmColor;
            [System.Runtime.InteropServices.FieldOffset(62)]
            public short dmDuplex;
            [System.Runtime.InteropServices.FieldOffset(64)]
            public short dmYResolution;
            [System.Runtime.InteropServices.FieldOffset(66)]
            public short dmTTOption;
            [System.Runtime.InteropServices.FieldOffset(68)]
            public short dmCollate;
            [System.Runtime.InteropServices.FieldOffset(72)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            [System.Runtime.InteropServices.FieldOffset(102)]
            public Int16 dmLogPixels;
            [System.Runtime.InteropServices.FieldOffset(104)]
            public Int32 dmBitsPerPel;
            [System.Runtime.InteropServices.FieldOffset(108)]
            public Int32 dmPelsWidth;
            [System.Runtime.InteropServices.FieldOffset(112)]
            public Int32 dmPelsHeight;
            [System.Runtime.InteropServices.FieldOffset(116)]
            public Int32 dmDisplayFlags;
            [System.Runtime.InteropServices.FieldOffset(116)]
            public Int32 dmNup;
            [System.Runtime.InteropServices.FieldOffset(120)]
            public Int32 dmDisplayFrequency;


            [System.Runtime.InteropServices.FieldOffset(124)]
            public Int32 dmICMMethod;

            [System.Runtime.InteropServices.FieldOffset(128)]
            public Int32 dmICMIntent;

            [System.Runtime.InteropServices.FieldOffset(132)]
            public Int32 dmMediaType;

            [System.Runtime.InteropServices.FieldOffset(136)]
            public Int32 dmDitherType;

            [System.Runtime.InteropServices.FieldOffset(140)]
            public Int32 dmReserved1;

            [System.Runtime.InteropServices.FieldOffset(144)]
            public Int32 dmReserved2;

            [System.Runtime.InteropServices.FieldOffset(148)]
            public Int32 dmPanningWidth;

            [System.Runtime.InteropServices.FieldOffset(152)]
            public Int32 dmPanningHeight;
        }

        public struct POINTL
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInfo
        {
            /// <summary>
            /// The size, in bytes, of the structure. Set this member to sizeof(MONITORINFO) (40) before calling the GetMonitorInfo function. 
            /// Doing so lets the function determine the type of structure you are passing to it.
            /// </summary>
            public int Size;

            /// <summary>
            /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates. 
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public RectStruct Monitor;

            /// <summary>
            /// A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications, 
            /// expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor. 
            /// The rest of the area in rcMonitor contains system windows such as the task bar and side bars. 
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public RectStruct WorkArea;

            /// <summary>
            /// The attributes of the display monitor.
            /// 
            /// This member can be the following value:
            ///   1 : MONITORINFOF_PRIMARY
            /// </summary>
            public uint Flags;

            public void Init()
            {
                this.Size = 40;
            }
        }

        /// <summary>
        /// The MONITORINFOEX structure contains information about a display monitor.
        /// The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
        /// The MONITORINFOEX structure is a superset of the MONITORINFO structure. The MONITORINFOEX structure adds a string member to contain a name 
        /// for the display monitor.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx
        {
            /// <summary>
            /// The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the GetMonitorInfo function. 
            /// Doing so lets the function determine the type of structure you are passing to it.
            /// </summary>
            public int Size;

            /// <summary>
            /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates. 
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public RectStruct Monitor;

            /// <summary>
            /// A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications, 
            /// expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor. 
            /// The rest of the area in rcMonitor contains system windows such as the task bar and side bars. 
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public RectStruct WorkArea;

            /// <summary>
            /// The attributes of the display monitor.
            /// 
            /// This member can be the following value:
            ///   1 : MONITORINFOF_PRIMARY
            /// </summary>
            public uint Flags;

            /// <summary>
            /// A string that specifies the device name of the monitor being used. Most applications have no use for a display monitor name, 
            /// and so can save some bytes by using a MONITORINFO structure.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string DeviceName;

            public void Init()
            {
                this.Size = 40 + 2 * CCHDEVICENAME;
                this.DeviceName = string.Empty;
            }
        }

        /// <summary>
        /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/dd162897%28VS.85%29.aspx"/>
        /// <remarks>
        /// By convention, the right and bottom edges of the rectangle are normally considered exclusive. 
        /// In other words, the pixel whose coordinates are ( right, bottom ) lies immediately outside of the the rectangle. 
        /// For example, when RECT is passed to the FillRect function, the rectangle is filled up to, but not including, 
        /// the right column and bottom row of pixels. This structure is identical to the RECTL structure.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct RectStruct
        {
            /// <summary>
            /// The x-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Left;

            /// <summary>
            /// The y-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Top;

            /// <summary>
            /// The x-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Right;

            /// <summary>
            /// The y-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Bottom;
        }

        #endregion
    }
}
