// File: CompressedImageControl.xaml.cs
// Project: ROS_ImageWPF
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

//using System.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ros_CSharp;
using d = System.Drawing;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;

#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///     A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    public partial class CompressedImageControl : UserControl
    {
        public event FPSEvent fpsevent;

        public delegate void ImageReceivedHandler(CompressedImageControl sender);

        public static string newTopicName;

        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof (string),
            typeof (CompressedImageControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is CompressedImageControl)
                        {
                            CompressedImageControl target = obj as CompressedImageControl;
                            target.Topic = (string) args.NewValue;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }));

        private List<SlaveImage> _slaves = new List<SlaveImage>();
        private NodeHandle imagehandle;
        private Subscriber<sm.CompressedImage> imgsub;
        private Thread waitforinit;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImageControl" /> class.
        ///     constructor... nothing fancy
        /// </summary>
        public CompressedImageControl()
        {
            InitializeComponent();
            guts.fpsevent += (fps) => { if (fpsevent != null) fpsevent(fps); };
        }

        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set
            {
                Console.WriteLine("CHANGING TOPIC FROM " + Topic + " to " + value);
                SetValue(TopicProperty, value);
                Init();
            }
        }

        public SolidColorBrush ColorConverter(m.ColorRGBA c)
        {
            return new SolidColorBrush(Color.FromArgb(128, (byte) Math.Round(c.r), (byte) Math.Round(c.g), (byte) Math.Round(c.b)));
        }

        public Rectangle DrawABox(Point topleft, double width, double height, double imgwidth, double imgheight, m.ColorRGBA color)
        {
            if (ActualWidth < 1 || ActualHeight < 1) return null;
            Point tl = new Point(topleft.X*imgwidth/ActualWidth, topleft.Y*imgheight/ActualHeight);
            Point br = new Point((topleft.X + width)*imgwidth/ActualWidth, (topleft.Y + height)*imgheight/ActualHeight);
            ;
            Rectangle r = new Rectangle {Width = br.X - tl.X, Height = br.Y - tl.Y, Stroke = Brushes.White, Fill = ColorConverter(color), StrokeThickness = 1, Opacity = 1.0};
            r.SetValue(Canvas.LeftProperty, tl.X);
            r.SetValue(Canvas.TopProperty, tl.Y);
            ROI_Container.Children.Add(r);
            return r;
        }

        public bool EraseABox(Rectangle r)
        {
            if (ROI_Container.Children.Contains(r))
            {
                ROI_Container.Children.Remove(r);
                return true;
            }
            return false;
        }

        public event ImageReceivedHandler ImageReceivedEvent;

        public void AddSlave(SlaveImage s)
        {
            _slaves.Add(s);
        }

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


        private void waitfunc(string TopicName)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            SetupTopic(TopicName);
        }

        private void SetupTopic(string TopicName)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
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
            imgsub = imagehandle.subscribe<sm.CompressedImage>(TopicName, 1, UpdateImage);
        }

        public void UpdateImage(sm.CompressedImage i)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UpdateImage(ref i.data);
                foreach (SlaveImage si in _slaves)
                    si.UpdateImage(i.data);
                if (ImageReceivedEvent != null)
                    ImageReceivedEvent(this);
            }));
        }

        /// <summary>
        ///     Uses a memory stream to turn a byte array into a BitmapImage via helper method, BytesToImage, then passes the image
        ///     to UpdateImage(BitmapImage)
        /// </summary>
        /// <param name="data">
        /// </param>
        public void UpdateImage(ref byte[] data)
        {
            guts.UpdateImage(data);
        }

        #region Events

        private const double _scalex = -1;
        private const double _scaley = 1;

        /// <summary>
        ///     when going from a System.Drawing.Bitmap's byte array, throwing a bmp file header on it, and sticking it in a
        ///     BitmapImage with a MemoryStream,
        ///     the image gets flipped upside down from how it would look in a  PictureBox in a Form, so this transform corrects
        ///     that inversion
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
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