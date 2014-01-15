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
    public partial class MapControl : UserControl
    {
        public WindowPositionStuff WhereYouAt(UIElement uie)
        {
            System.Windows.Point tl = new System.Windows.Point(), br = new System.Windows.Point();
            tl = TranslatePoint(new System.Windows.Point(), uie);
            br = TranslatePoint(new System.Windows.Point(Width, Height), uie);
            return new WindowPositionStuff(tl, br, new Size(Math.Abs(br.X - tl.X), Math.Abs(br.Y - tl.Y)));
        }
        public System.Windows.Point origin = new System.Windows.Point(0, 0);
        //pixels per meter, and meters per pixel respectively. This is whatever you have the map set to on the ROS side. These variables are axtually wrong, PPM is meters per pixel. Will fix...
        private static float _PPM = 0.02868f;
        private static float _MPP = 1.0f / PPM;
        public static float MPP
        {
            get { return _MPP; }
            set
            {
                _MPP = value;
                _PPM = 1.0f / value;
            }
        }
        public static float PPM
        {
            get { return _PPM; }
            set
            {
                _PPM = value;
                _MPP = 1.0f / value;
            }

        }

        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }
        private Thread waitforinit;
        private NodeHandle imagehandle;
        private Subscriber<nm.OccupancyGrid> mapsub;
        
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
            typeof(MapControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is MapControl)
                        {
                            MapControl target = obj as MapControl;
                            target.Topic = (string)args.NewValue;
                            target.Init();
                        }
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                }));

        private void waitfunc(string topic)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);
            SetupTopic(topic);
        }

        private void SetupTopic(string topic)
        {
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (mapsub != null || mapsub.topic != topic)
            {
                mapsub.shutdown();
                mapsub = null;
            }
            if (mapsub != null)
                return;
            Console.WriteLine("MAP TOPIC = " + topic);            
            mapsub = imagehandle.subscribe<nm.OccupancyGrid>(topic, 1, (i) => Dispatcher.Invoke(new Action(() =>
                                                                                                                        {
                                                                                                                            SmartResize(i.info.width, i.info.height);
                                                                                                                            MPP = i.info.resolution;
                                                                                                                            this.origin = new System.Windows.Point(i.info.origin.position.x,i.info.origin.position.y);
                                                                                                                            Size s = new Size(i.info.width, i.info.height);
                                                                                                                            byte[] d = createRGBA(i.data);
                                                                                                                            guts.UpdateImage(ref d, s, false);
                                                                                                                            d = null;
                                                                                                                        })));
        }

        private void SmartResize(uint w, uint h)
        {
            //determine aspect ratio
            double iar = w / h;
            
            //determine own aspect ratio
            double ar = Width / Height;

            //do nothing if close enough
            if (Math.Abs(ar - iar) < 0.001)
                return;
            
            Height = Width / iar;
        }

        private byte[] createRGBA(sbyte[] map)
        {
            byte[] image = new byte[4 * map.Length];
            int count = 0;
            foreach (sbyte j in map)
            {
                switch (j)
                {
                    case -1:
                        image[count] = 211;
                        image[count + 1] = 211;
                        image[count + 2] = 211;
                        image[count + 3] = 0xFF;
                        break;
                    case 100:
                        image[count] = 105;
                        image[count + 1] = 105;
                        image[count + 2] = 105;
                        image[count + 3] = 0xFF;
                        break;
                    case 0:
                        image[count] = 255;
                        image[count+1] = 255;
                        image[count+2] = 255;
                        image[count + 3] = 0xFF;
                        break;
                    default:
                        image[count] = 255;
                        image[count+1] = 0;
                        image[count+2] = 0;
                        image[count + 3] = 0xFF;
                        break;
                }
                count += 4;
            }
            return image;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public MapControl()
        {
            InitializeComponent();
            guts.fps.Visibility = Visibility.Hidden;
        }
    }

    public class WindowPositionStuff
    {
        public System.Windows.Point TL, BR;
        public System.Windows.Size size;

        public WindowPositionStuff(System.Windows.Point tl, System.Windows.Point br, Size s)
        {
            // TODO: Complete member initialization
            this.TL = tl;
            this.BR = br;
            this.size = s;
        }
    }
}