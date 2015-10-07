// File: WindowScraper.cs
// Project: ROS_ImageWPF
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/01/2015
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Messages.sensor_msgs;
using Ros_CSharp;
using Header = Messages.std_msgs.Header;
using Image = System.Drawing.Image;
using String = Messages.std_msgs.String;

#endregion

namespace ROS_ImageWPF
{
    public class WindowScraper
    {
        private bool enabled;
        private IntPtr hwnd;
        private NodeHandle nh;
        private int period_ms = Timeout.Infinite;
        private Publisher<CompressedImage> pub;
        private Timer timer;
        private int window_left, window_top;

        public WindowScraper(string topic, Window w, int hz = 1) : this(topic, w, TimeSpan.FromSeconds(1.0/hz))
        {
        }

        public WindowScraper(string topic, Window w, TimeSpan period)
        {
            w.Dispatcher.Invoke(new Action(() =>
                                               {
                                                   w.LocationChanged += (s, a) =>
                                                                            {
                                                                                window_left = (int) w.Left;
                                                                                window_top = (int) w.Top;
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
        }

        private void callback(object o)
        {
            CompressedImage cm = new CompressedImage {format = new String("jpeg"), header = new Header {stamp = ROS.GetTime()}};
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
                Image.FromHbitmap(new IntPtr(hBitmap)).Save(str, imageFormat);
            }

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
        }
    }
}