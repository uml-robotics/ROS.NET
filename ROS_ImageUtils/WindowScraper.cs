using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Messages.sensor_msgs;
using Ros_CSharp;

namespace ROS_ImageWPF
{
    public class WindowScraper
    {
        private Timer timer;
        private Publisher<CompressedImage> pub;
        private Window window;
        private int dpi;
        private int period_ms = Timeout.Infinite;
        private bool running = false;

        public WindowScraper(string topic, Window w, int hz = 1, int dpi = 96) : this(topic, w, TimeSpan.FromSeconds(1.0/((double)hz)), dpi)
        {
        }
        
        public WindowScraper(string topic, Window w, TimeSpan period, int dpi = 96)
        {
            window = w;
            this.dpi = dpi;
            period_ms = (int) Math.Floor(period.TotalMilliseconds);
            timer = new Timer(callback, null, Timeout.Infinite, period_ms);
            NodeHandle nh = new NodeHandle();
            pub = nh.advertise<CompressedImage>(topic, 1, true);
        }

        public void Start()
        {
            if (!running)
            {
                running = true;
                timer.Change(0, period_ms);
            }
        }

        public void Stop()
        {
            if (running)
            {
                timer.Change(Timeout.Infinite, period_ms);
                running = false;
            }
        }

        public void Shutdown()
        {
            Stop();
            pub.shutdown();
        }

        private void callback(object o)
        {
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                #region based on http://blogs.msdn.com/b/saveenr/archive/2008/09/18/wpf-xaml-saving-a-window-or-canvas-as-a-png-bitmap.aspx

                var rtb = new RenderTargetBitmap(
                    (int) window.Width, //width 
                    (int) window.Width, //height 
                    dpi, //dpi x 
                    dpi, //dpi y 
                    PixelFormats.Pbgra32 // pixelformat 
                    );
                rtb.Render(window);

                var enc = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

                #endregion

                CompressedImage cm = new CompressedImage {format = new Messages.std_msgs.String("jpeg"), header = new Messages.std_msgs.Header {stamp = ROS.GetTime()}};
                using (MemoryStream ms = new MemoryStream())
                {
                    enc.Save(ms);
                    cm.data = ms.GetBuffer();
                }
                pub.publish(cm);
            }));
        }
    }
}
