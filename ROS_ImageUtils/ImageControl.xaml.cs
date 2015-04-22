// File: ImageControl.xaml.cs
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

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
    public partial class ImageControl : UserControl
    {
        public event FPSEvent fpsevent;

        public delegate void ImageReceivedHandler(ImageControl sender);

        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof (string),
            typeof (ImageControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is ImageControl)
                        {
                            ImageControl target = obj as ImageControl;
                            target.Topic = (string) args.NewValue;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }));

        private NodeHandle imagehandle;
        private Subscriber<sm.Image> imgsub;
        private Thread waitforinit;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImageControl" /> class.
        ///     constructor... nothing fancy
        /// </summary>
        public ImageControl()
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

        public event ImageReceivedHandler ImageReceivedEvent;

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
            if (imgsub != null)
                return;
            imgsub = imagehandle.subscribe<sm.Image>(TopicName, 1, UpdateImage);
        }

        #region Events

        private void UpdateImage(sm.Image i)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (guts == null)
                    return;
                guts.UpdateImage(i.data, new Size((int) i.width, (int) i.height), false, i.encoding.data);
                if (ImageReceivedEvent != null) ImageReceivedEvent(this);
            }));
        }

        private const double _scalex = 1;
        private const double _scaley = -1;

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