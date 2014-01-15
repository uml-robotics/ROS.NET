#region

using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
using System.Linq;
#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///   A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    /// 

    public partial class CompressedImageControl : UserControl
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public CompressedImageControl()
        {
            InitializeComponent();
        }

        public SolidColorBrush ColorConverter(Messages.std_msgs.ColorRGBA c)
        {
            return new SolidColorBrush(Color.FromArgb(128, (byte)Math.Round((double)c.r), (byte)Math.Round((double)c.g), (byte)Math.Round((double)c.b)));
        }

        public Rectangle DrawABox(System.Windows.Point topleft, double width, double height, double imgwidth, double imgheight, Messages.std_msgs.ColorRGBA color)
        {
            if (ActualWidth < 1 || ActualHeight < 1) return null;
            System.Windows.Point tl = new System.Windows.Point(topleft.X * imgwidth / ActualWidth, topleft.Y * imgheight / ActualHeight);
            System.Windows.Point br = new System.Windows.Point((topleft.X + width) * imgwidth / ActualWidth, (topleft.Y + height) * imgheight / ActualHeight); ;
            System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle() { Width = br.X - tl.X, Height = br.Y - tl.Y, Stroke = Brushes.White, Fill = ColorConverter(color), StrokeThickness = 1, Opacity = 1.0 };
            r.SetValue(Canvas.LeftProperty, (object)tl.X);
            r.SetValue(Canvas.TopProperty, (object)tl.Y);
            ROI_Container.Children.Add(r);
            return r;
        }

        public bool EraseABox(System.Windows.Shapes.Rectangle r)
        {
            if (ROI_Container.Children.Contains(r))
            {
                ROI_Container.Children.Remove(r);
                return true;
            }
            return false;
        }

        public delegate void ImageReceivedHandler(CompressedImageControl sender);
        public event ImageReceivedHandler ImageReceivedEvent;
        private List<SlaveImage> _slaves = new List<SlaveImage>();
        public void AddSlave(SlaveImage s)
        {
            _slaves.Add(s);
        }
        public static string newTopicName;
        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set { Console.WriteLine("CHANGING TOPIC FROM " + Topic + " to " + value); SetValue(TopicProperty, value); Init();}
        }
        private Thread waitforinit;
        private NodeHandle imagehandle;
        private Subscriber<sm.CompressedImage> imgsub;
        public void shutdown()
        {
            if (imgsub != null)
            {
                imgsub.shutdown();
                imgsub = null;
            }
            if (imagehandle != null)
            {
                imagehandle.shutdown();
                imagehandle = null;
            }
        }

        private void Init()
        {
            if (!ROS.isStarted())
            {
                if (waitforinit == null)
                {
                    string workaround = Topic;
                    waitforinit = new Thread(() => waitfunc(workaround));
                }
                if (!waitforinit.IsAlive)
                {
                    waitforinit.Start();
                }
            }
            else
                SetupTopic(Topic);
        }


        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof(string),
            typeof(CompressedImageControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is CompressedImageControl)
                        {
                            CompressedImageControl target = obj as CompressedImageControl;
                            target.Topic = (string)args.NewValue;
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
        private void SetupTopic(string TopicName)
        {
            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (imgsub != null && imgsub.topic != TopicName)
            {
                imgsub.shutdown();
                imgsub = null;
            }
            Console.WriteLine("IMG TOPIC " + TopicName);
            if (imgsub != null)
                return;
            imgsub = imagehandle.subscribe<sm.CompressedImage>(new SubscribeOptions<sm.CompressedImage>(TopicName, 1, (i) => Dispatcher.Invoke(new Action(() =>
            {
                UpdateImage(ref i.data);
                foreach (SlaveImage si in _slaves)
                    si.UpdateImage(ref i.data);
                if (ImageReceivedEvent != null)
                    ImageReceivedEvent(this);
            }))) { allow_concurrent_callbacks = true });
        }

        /// <summary>
        ///   Uses a memory stream to turn a byte array into a BitmapImage via helper method, BytesToImage, then passes the image to UpdateImage(BitmapImage)
        /// </summary>
        /// <param name = "data">
        /// </param>
        public void UpdateImage(ref byte[] data)
        {
            guts.UpdateImage(ref data);
        }

        #region Events

        private const double _scalex = -1;
        private const double _scaley = 1;

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