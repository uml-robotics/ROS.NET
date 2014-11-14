using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Messages.sensor_msgs;
using Ros_CSharp;

namespace ROS_ImageWPF
{
    public class WindowScraper
    {
        private DispatcherTimer timer;
        private Publisher<CompressedImage> pub;
        private Window window;
        private int period_ms = Timeout.Infinite;
        private bool running = false;

        public WindowScraper(string topic, Window w, int hz = 1) : this(topic, w, TimeSpan.FromSeconds(1.0/((double)hz)))
        {
        }
        
        public WindowScraper(string topic, Window w, TimeSpan period)
        {
            window = w;
            period_ms = (int) Math.Floor(period.TotalMilliseconds);
            timer = new DispatcherTimer(period, DispatcherPriority.Background, callback, window.Dispatcher);
            NodeHandle nh = new NodeHandle();
            pub = nh.advertise<CompressedImage>(topic, 1, false);
        }

        public void Start()
        {
            if (!timer.IsEnabled)
            {
                timer.Start();
            }
        }

        public void Stop()
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
        }

        public void Shutdown()
        {
            Stop();
            pub.shutdown();
        }

        private void callback(object o, EventArgs args)
        {
            double dpiX = -1.0, dpiY = -1.0;
            CompressedImage cm = new CompressedImage { format = new Messages.std_msgs.String("jpeg"), header = new Messages.std_msgs.Header { stamp = ROS.GetTime() } };

            #region based on

            PresentationSource source = PresentationSource.FromVisual(window);

            if (source != null)
            {
                dpiX = 96.0*source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0*source.CompositionTarget.TransformToDevice.M22;
            }

            #endregion

            if (dpiX >= 0 && dpiY >= 0)
            {
                
                #region based on http://blogs.msdn.com/b/saveenr/archive/2008/09/18/wpf-xaml-saving-a-window-or-canvas-as-a-png-bitmap.aspx

                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int) window.ActualWidth, //width 
                    (int) window.ActualHeight, //height 
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

                pub.publish(cm);
            }
        }
    }
}
