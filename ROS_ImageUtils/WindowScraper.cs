using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Messages.sensor_msgs;
using Microsoft.Win32.SafeHandles;
using Ros_CSharp;
using Image = Messages.sensor_msgs.Image;

namespace ROS_ImageWPF
{
    public class WindowScraper
    {
        private Timer timer;
        private Publisher<CompressedImage> pub;
        private int period_ms = Timeout.Infinite;
        private bool enabled = false;
        private NodeHandle nh;
        private IntPtr hwnd;
        private int window_left, window_top;

        public WindowScraper(string topic, Window w, int hz = 1) : this(topic, w, TimeSpan.FromSeconds(1.0/((double) hz)))
        {
        }

        public WindowScraper(string topic, Window w, TimeSpan period)
        {

            w.Dispatcher.Invoke(new Action(() =>
            {
                w.LocationChanged += (s, a) => {
                    window_left = (int)w.Left;
                    window_top = (int)w.Top;
                };
            }));
            hwnd = new WindowInteropHelper(w).Handle;
            period_ms = (int) Math.Floor(period.TotalMilliseconds);
            timer = new Timer(callback, null, Timeout.Infinite, period_ms);
            nh = new NodeHandle();
            if (!topic.EndsWith("/compressed"))
            {
                Console.WriteLine("APPENDING /compressed TO TOPIC NAME TO MAKE IMAGE TRANSPORT HAPPY");
                topic += "/compressed";
            }
            pub = nh.advertise<CompressedImage>(topic, 10);
        }

        public void Start()
        {
            if (!enabled)
            {
                enabled = true;
                timer.Change(0, period_ms);
            }
        }

        public void Stop()
        {
            if (enabled)
            {
                enabled = false;
                timer.Change(Timeout.Infinite, period_ms);
            }
        }

        public void Shutdown()
        {
            Stop();
            pub.shutdown();
            ROS.shutdown();
            ROS.waitForShutdown();
        }

        private void callback(object o)
        {
            CompressedImage cm = new CompressedImage { format = new Messages.std_msgs.String("jpeg"), header = new Messages.std_msgs.Header { stamp = ROS.GetTime() } };
            using (MemoryStream ms = new MemoryStream())
            {
                PInvoke.CaptureWindow(hwnd, window_left, window_top, ms, ImageFormat.Jpeg);
                cm.data = ms.GetBuffer();
            }
            pub.publish(cm);
        }

        private static class PInvoke
        {
            /* Author: Perry Lee
            * Submission: Capture Screen (Add Screenshot Capability to Programs)
            * Date of Submission: 12/29/03
            */

            private static class GDI32
            {
                [DllImport("GDI32.dll")]
                public static extern bool BitBlt(int hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, int hdcSrc, int nXSrc, int nYSrc, int dwRop);

                [DllImport("GDI32.dll")]
                public static extern int CreateCompatibleBitmap(int hdc, int nWidth, int nHeight);

                [DllImport("GDI32.dll")]
                public static extern int CreateCompatibleDC(int hdc);

                [DllImport("GDI32.dll")]
                public static extern bool DeleteDC(int hdc);

                [DllImport("GDI32.dll")]
                public static extern bool DeleteObject(int hObject);

                [DllImport("GDI32.dll")]
                public static extern int GetDeviceCaps(int hdc, int nIndex);

                [DllImport("GDI32.dll")]
                public static extern int SelectObject(int hdc, int hgdiobj);
            }

            public static void CaptureWindow(IntPtr hwnd, int winx, int winy, Stream str, ImageFormat imageFormat)
            {
                int hdcSrc = 0;
                Graphics g = Graphics.FromHwnd(hwnd);
                hdcSrc = (int) g.GetHdc();
                if (hdcSrc != 0)
                {
                    int hdcDest = GDI32.CreateCompatibleDC(hdcSrc),
                        hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc,
                            GDI32.GetDeviceCaps(hdcSrc, 8), GDI32.GetDeviceCaps(hdcSrc, 10));
                    GDI32.SelectObject(hdcDest, hBitmap);
                    GDI32.BitBlt(hdcDest, 0, 0, GDI32.GetDeviceCaps(hdcSrc, 8),
                        GDI32.GetDeviceCaps(hdcSrc, 10), hdcSrc, winx, winy, 0x00CC0020);
                    SaveImageAs(hBitmap, str, imageFormat);
                    Cleanup(hBitmap, hdcSrc, hdcDest);
                }
                if (g != null)
                    g.Dispose();
            }

            private static void Cleanup(int hBitmap, int hdcSrc, int hdcDest)
            {
                GDI32.DeleteDC(hdcDest);
                GDI32.DeleteObject(hBitmap);
            }

            private static void SaveImageAs(int hBitmap, Stream str, ImageFormat imageFormat)
            {
                System.Drawing.Image.FromHbitmap(new IntPtr(hBitmap)).Save(str, imageFormat);
            }
        }
    }
}
