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

        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }
        private Thread waitforinit;
        private static NodeHandle imagehandle;
        private Subscriber<nm.OccupancyGrid> mapsub;

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
                            target.TopicName = (string)args.NewValue;
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
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);
            Dispatcher.Invoke(new Action(SetupTopic));
        }

        private void SetupTopic()
        {
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (mapsub != null)
                mapsub.shutdown();
            Console.WriteLine("MAP TOPIC = " + TopicName);            
            mapsub = imagehandle.subscribe<nm.OccupancyGrid>(TopicName, 1, (i) => Dispatcher.Invoke(new Action(() =>
                                                                                                                        {
                                                                                                                            //this.Height = i.info.height;
                                                                                                                            //this.Width = i.info.width;
                                                                                                                            MPP = i.info.resolution;
                                                                                                                            this.origin = new System.Windows.Point(i.info.origin.position.x,i.info.origin.position.y);
                                                                                                                            //sbyte[] data;
                                                                                                                            Size s = new Size(i.info.width, i.info.height);
                                                                                                                            //data = findROI(i.data, ref s);
                                                                                                                            byte[] d = createRGBA(i.data);
                                                                                                                            guts.UpdateImage(ref d, s, false);
                                                                                                                            d = null;
                                                                                                                        })));
        }

        private sbyte[] findROI(sbyte[] p, ref Size s)
        {
            int minx, miny, maxx, maxy;
            minx = miny = int.MaxValue;
            maxx = maxy = int.MinValue;
            int w=(int)s.Width;
            int h=(int)s.Height;
            for(int y=0;y<h;y++)
                for (int x = 0; x < w; x++)
                {
                    if (p[x + y * w] != -1)
                    {
                        if (x < minx)
                            minx = x;
                        else if (x > maxx)
                            maxx = x;
                        if (y < miny)
                            miny = y;
                        else if (y > maxy)
                            maxy = y;
                    }
                }
            maxx = Math.Min(maxx + 20, w);
            maxy = Math.Min(maxy + 20, h);
            minx = Math.Max(minx - 20, 0);
            miny = Math.Max(miny - 20, 0);
            s = new Size(maxx - minx + 1, maxy - miny + 1);
            sbyte[] output = new sbyte[(maxx - minx + 1) * (maxy - miny + 1)];
            for(int y=0;y<maxy-miny+1;y++)
                for (int x = 0; x < maxx - minx+1; x++)
                {
                    output[x + y * (maxx - minx + 1)] = p[(x + minx) + (y + miny) * w];
                }
            return output;
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