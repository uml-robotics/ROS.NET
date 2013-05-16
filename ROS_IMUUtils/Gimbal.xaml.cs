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

namespace ROS_IMUUtil
{
    /// <summary>
    ///   A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    /// 
    
    public partial class Gimbal : UserControl
    {
        public static string newTopicName;
        DateTime wtf;
        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }
        private Thread waitforinit;
        private static NodeHandle imagehandle;
        //private Subscriber<sm.Image> imgsub;


        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof(string),
            typeof(Gimbal),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is Gimbal)
                        {
                            Gimbal target = obj as Gimbal;
                            target.TopicName = (string)args.NewValue;
                            if (newTopicName != null)
                                target.TopicName = newTopicName;
                            if (!ROS.isStarted())
                            {
                                if (target.waitforinit == null)
                                    target.waitforinit = new Thread(new ThreadStart(target.waitfunc));
                                if (!target.waitforinit.IsAlive)
                                {
                                    target.waitforinit.Start();
                                }
                            }
                            else
                                target.SetupTopic();
                        }
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                }));

        private void waitfunc()
        {
            while (!ROS.initialized)
            {
                Thread.Sleep(100);
            }
            Dispatcher.BeginInvoke(new Action(SetupTopic));
        }

        private Thread spinnin;
        private void SetupTopic()
        {
            if (imagehandle == null)
                imagehandle = new NodeHandle();
           // if (imgsub != null)
           //     imgsub.shutdown();
            wtf = DateTime.Now;

            /*imgsub = imagehandle.subscribe<sm.Image>(TopicName, 1, (i) =>
                Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateImage(i.data, new Size((int)i.width, (int)i.height), false, i.encoding.data);
                        if (ImageReceivedEvent != null) ImageReceivedEvent(this);
                    })));*/
            if (spinnin == null)
            {
                spinnin = new Thread(new ThreadStart(() => {ROS.spinOnce(imagehandle); Thread.Sleep(100); })); spinnin.Start();
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Gimbal" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public Gimbal()
        {
            InitializeComponent();
        }

        #region Events

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
        }

        #endregion

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }
    }
}