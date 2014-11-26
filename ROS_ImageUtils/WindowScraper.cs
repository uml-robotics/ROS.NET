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
        private Window window;
        private int period_ms = Timeout.Infinite;
        private bool running = false;
        private Queue<CompressedImage> outboundqueue = new Queue<CompressedImage>();
        private Thread queueworker;
        private double dpiX, dpiY;
        private double WIDTH, HEIGHT;
        private bool enabled = false;
        private Semaphore queuesem = new Semaphore(0, 1);

        public WindowScraper(string topic, Window w, int hz = 1) : this(topic, w, TimeSpan.FromSeconds(1.0/((double) hz)))
        {
        }

        public WindowScraper(string topic, Window w, TimeSpan period)
        {
            window = w;
            window.Dispatcher.Invoke(new Action(() =>
            {
                w.SizeChanged += new SizeChangedEventHandler(w_SizeChanged);
                WIDTH = w.RenderSize.Width;
                HEIGHT = w.RenderSize.Height;
            }));
            period_ms = (int) Math.Floor(period.TotalMilliseconds);
            timer = new Timer(callback, null, Timeout.Infinite, period_ms);
            NodeHandle nh = new NodeHandle();
            if (!topic.EndsWith("/compressed"))
            {
                Console.WriteLine("APPENDING /compressed TO TOPIC NAME TO MAKE IMAGE TRANSPORT HAPPY");
                topic += "/compressed";
            }
            pub = nh.advertise<CompressedImage>(topic, 1);
            queueworker = new Thread(() =>
            {
                Queue<CompressedImage> localqueue = new Queue<CompressedImage>();
                while (ROS.ok)
                {
                    queuesem.WaitOne();
                    lock (outboundqueue)
                    {
                        while (outboundqueue.Count > 0)
                            localqueue.Enqueue(outboundqueue.Dequeue());
                    }
                    CompressedImage cm;
                    while (localqueue.Count > 0 && (cm = localqueue.Dequeue()) != null)
                    {
                        pub.publish(cm);
                    }
                }
            });
            queueworker.Start();
        }

        private void w_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WIDTH = e.NewSize.Width;
            HEIGHT = e.NewSize.Height;
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
            queuesem.WaitOne(0);
            queuesem.Release();
            queueworker.Join();
        }

        private void callback(object o)
        {
            DateTime start = DateTime.Now;
            CompressedImage cm = new CompressedImage {format = new Messages.std_msgs.String("jpeg"), header = new Messages.std_msgs.Header {stamp = ROS.GetTime()}};

            if (dpiX <= 0 || dpiY <= 0)
            {
                window.Dispatcher.Invoke(new Action(() =>
                {
                    PresentationSource source = PresentationSource.FromVisual(window);

                    if (source != null)
                    {
                        dpiX = 96.0*source.CompositionTarget.TransformToDevice.M11;
                        dpiY = 96.0*source.CompositionTarget.TransformToDevice.M22;
                    }
                }));
            }



            /*window.Dispatcher.BeginInvoke(new Action(() =>
            {
                #region based on http://blogs.msdn.com/b/saveenr/archive/2008/09/18/wpf-xaml-saving-a-window-or-canvas-as-a-png-bitmap.aspx

                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int) WIDTH, //width 
                    (int) HEIGHT, //height 
                    dpiX, //dpi x 
                    dpiY, //dpi y 
                    PixelFormats.Pbgra32 // pixelformat 
                    );
                rtb.Render(window);

                JpegBitmapEncoder enc = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

                using (MemoryStream ms = new MemoryStream())
                {
                    enc.Save(ms);
                    cm.data = ms.GetBuffer();
                }

                #endregion
             */

            window.Dispatcher.BeginInvoke(new Action(() =>
                {
            using (MemoryStream ms = new MemoryStream())
            {
                PInvoke.CaptureWindow(window, ms, ImageFormat.Jpeg);
                cm.data = ms.GetBuffer();
            }

            lock (outboundqueue)
                outboundqueue.Enqueue(cm);

            queuesem.WaitOne(0);
            try
            {
                queuesem.Release();
            }
            catch (SemaphoreFullException ex)
            {
                //noop
            }
            }));
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

            private static class User32
            {
                [DllImport("User32.dll")]
                public static extern int GetDesktopWindow();

                [DllImport("User32.dll")]
                public static extern int GetWindowDC(int hWnd);

                [DllImport("User32.dll")]
                public static extern int ReleaseDC(int hWnd, int hDC);
            }

            public static void CaptureWindow(Window w, Stream str, ImageFormat imageFormat)
            {
                int hdcSrc = 0;
                Graphics g = null;
                WindowInteropHelper wih = new WindowInteropHelper(w);
                IntPtr hwnd = wih.Handle;
                g = System.Drawing.Graphics.FromHwnd(hwnd);
                hdcSrc = (int) g.GetHdc();
                if (hdcSrc != 0)
                {
                    int hdcDest = GDI32.CreateCompatibleDC(hdcSrc),
                        hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc,
                            GDI32.GetDeviceCaps(hdcSrc, 8), GDI32.GetDeviceCaps(hdcSrc, 10));
                    GDI32.SelectObject(hdcDest, hBitmap);
                    GDI32.BitBlt(hdcDest, 0, 0, GDI32.GetDeviceCaps(hdcSrc, 8),
                        GDI32.GetDeviceCaps(hdcSrc, 10), hdcSrc, 0, 0, 0x00CC0020);
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
                Bitmap image =
                    new Bitmap(System.Drawing.Image.FromHbitmap(new IntPtr(hBitmap)),
                        System.Drawing.Image.FromHbitmap(new IntPtr(hBitmap)).Width,
                        System.Drawing.Image.FromHbitmap(new IntPtr(hBitmap)).Height);
                image.Save(str, imageFormat);
            }
        }
    }
}
