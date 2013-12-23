#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;
using d = System.Drawing;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using PixelFormat = System.Windows.Media.PixelFormat;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///   A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    /// 
    
    public partial class ImageControl : UserControl
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public ImageControl()
        {
            InitializeComponent();
        }

        public delegate void ImageReceivedHandler(ImageControl sender);
        public event ImageReceivedHandler ImageReceivedEvent;

        public static string newTopicName;
        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }
        private Thread waitforinit;
        private static NodeHandle imagehandle;
        private Subscriber<sm.Image> imgsub;
        public void shutdown()
        {
            if (imagehandle != null)
                imagehandle.shutdown();
        }

        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof(string),
            typeof(ImageControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is ImageControl)
                        {
                            ImageControl target = obj as ImageControl;
                            target.TopicName = (string)args.NewValue;
                            if (newTopicName != null)
                                target.TopicName = newTopicName;
                            if (!ROS.isStarted())
                            {
                                if (target.waitforinit == null)
                                {
                                    string workaround = target.TopicName;
                                    target.waitforinit = new Thread(() => target.waitfunc(workaround));
                                }
                                if (!target.waitforinit.IsAlive)
                                {
                                    target.waitforinit.Start();
                                }
                            }
                            else
                                target.SetupTopic(target.TopicName);
                        }
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                }));

        private void waitfunc(string TopicName)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            SetupTopic(TopicName);
        }
        private Thread spinnin;
        private static object nhmut = new object();
        private void SetupTopic(string TopicName)
        {
            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            lock (nhmut)
            {
                if (imagehandle == null)
                    imagehandle = new NodeHandle();
            }
            if (imgsub != null)
            {
                imgsub.shutdown();
                imgsub = null;
            }
            imgsub = imagehandle.subscribe<sm.Image>(TopicName, 1, (i) =>
                Dispatcher.Invoke(new Action(() =>
                    {
                        if (guts == null)
                            return;
                        guts.UpdateImage(ref i.data, new Size((int)i.width, (int)i.height), false, i.encoding.data);
                        if (ImageReceivedEvent != null) ImageReceivedEvent(this);
                    })));
        }

        #region Events

        private const double _scalex = 1;
        private const double _scaley = -1;

        /// <summary>
        ///   when going from a System.Drawing.Bitmap's byte array, throwing a bmp file header on it, and sticking it in a BitmapImage with a MemoryStream,
        ///   the image gets flipped upside down from how it would look in a  PictureBox in a Form, so this transform corrects that inversion
        /// </summary>
        /// <param name = "sender">
        ///   The sender.
        /// </param>
        /// <param name = "e">
        ///   The e.
        /// </param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            guts.Transform(_scalex, _scaley);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            guts.Transform(_scalex, _scaley);
        }

        #endregion
    }
}