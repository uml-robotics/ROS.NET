#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private Subscriber<sm.Imu> imusub;


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

        private void imu_callback(sm.Imu i)
        {
            emQuaternion q = new emQuaternion(i.orientation);
            emMatrix3x3 mat = new emMatrix3x3(q);
            emMatrix3x3.OILER euler = mat.getEuler();
            //Console.WriteLine("" + euler.roll + " " + euler.pitch + " " + euler.yaw);

            Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (abraCadabra.Visibility != Visibility.Visible)
                        abraCadabra.Visibility = Visibility.Visible;
                    rotate(euler.pitch);
                    translate(euler.roll);
                }));
        }

        private void translate(double degrees)
        {
            double pixelsto90 = AngleMeter.ActualHeight / 2.0;
            trans.Y = pixelsto90 / 90.0;
            //Console.WriteLine(trans.Y);
        }

        private void rotate(double degrees)
        {
            Point transd = TransformToDescendant(AngleMeter).Transform(new Point(ActualWidth / 2.0, ActualHeight / 2.0));
            rot.CenterX = transd.X;
            rot.CenterY = transd.Y;
            rot.Angle = degrees * Math.PI / 180.0;
        }
        private void SetupTopic()
        {
            if (imagehandle == null)
                imagehandle = new NodeHandle();
           // if (imgsub != null)
           //     imgsub.shutdown();
            wtf = DateTime.Now;

            imusub = imagehandle.subscribe<sm.Imu>(TopicName, 1, imu_callback);
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
    }
}