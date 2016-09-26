// File: MapControl.xaml.cs
// Project: ROS_ImageWPF
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Linq;
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
    ///     A WPF Control used for displaying of maps (occupancy grids)
    ///     When added to a XAML file, the width of the map control needs to be set to the desired width, height will be set
    ///     automatically to keep map aspect ratio
    /// </summary>
    public partial class MapControl : UserControl
    {
        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof (string),
            typeof (MapControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                                                           {
                                                               try
                                                               {
                                                                   if (obj is MapControl)
                                                                   {
                                                                       MapControl target = obj as MapControl;
                                                                       target.Topic = (string) args.NewValue;
                                                                       target.DrawMap();
                                                                   }
                                                               }
                                                               catch (Exception e)
                                                               {
                                                                   Console.WriteLine(e);
                                                               }
                                                           }));

        private float actualResolution;
        private NodeHandle imagehandle;
        private float mapHeight;
        private float mapResolution; //in Meters per Pixel
        private Subscriber<nm.OccupancyGrid> mapSub;
        private float mapWidth;
        private Point origin;
        private Thread waitingThread;

        public MapControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Map Resolution in meters per pixel of the actual rendered map
        /// </summary>
        public float ActualResolution
        {
            get { return actualResolution; }
        }

        /// <summary>
        ///     Map Resolution in meters per pixel, (As provided in the map topic, not actual rendered resolution)
        /// </summary>
        public float MapResolution
        {
            get { return mapResolution; }
        }

        /// <summary>
        ///     Map height in pixels (This is the map height as provided in the map topic, not the actual rendered height)
        /// </summary>
        public float MapHeight
        {
            get { return mapHeight; }
        }

        /// <summary>
        ///     Map width in pixels (This is the map width as provided in the map topic, not the actual rendered width)
        /// </summary>
        public float MapWidth
        {
            get { return mapWidth; }
        }

        /// <summary>
        ///     Point set as the origin of the map
        /// </summary>
        public Point Origin
        {
            get { return origin; }
        }

        private string __topic = null;

        /// <summary>
        ///     Map provider topic
        /// </summary>
        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, (__topic = value)); }
        }

        private void DrawMap()
        {
            lock (this)
            {
                if (!ROS.isStarted())
                {
                    if (waitingThread == null)
                    {
                        waitingThread = new Thread(waitThenSubscribe);
                    }
                    if (!waitingThread.IsAlive)
                    {
                        waitingThread.Start();
                    }
                    return;
                }
            }
            if (!ROS.isStarted() || ROS.shutting_down)
                return;
            SubscribeToMap(Topic);
        }

        private void waitThenSubscribe()
        {
            while (true)
            {
                Thread.Sleep(100);
                lock (this)
                    if (ROS.shutting_down || ROS.isStarted())
                        break;
            }
            lock (this)
                if (ROS.shutting_down)
                    return;
            SubscribeToMap(__topic);
        }

        private void SubscribeToMap(string topic)
        {
            lock (this)
            {
                if (imagehandle == null)
                    imagehandle = new NodeHandle();
                if (mapSub != null && mapSub.topic != topic)
                {
                    mapSub.shutdown();
                    mapSub = null;
                }
                if (mapSub != null)
                    return;
                Console.WriteLine("Subscribing to map at:= " + topic);
                mapSub = imagehandle.subscribe<nm.OccupancyGrid>(topic, 1, i => Dispatcher.Invoke(new Action(() =>
                                                                                                                 {
                                                                                                                     Console.WriteLine("Map says its size is W: " + i.info.width + " H: " + i.info.height + " and its resolution is: " + i.info.resolution);
                                                                                                                     mapResolution = i.info.resolution;
                                                                                                                     mapHeight = i.info.height;
                                                                                                                     mapWidth = i.info.width;
                                                                                                                     if (Width != 0)
                                                                                                                         actualResolution = (mapWidth/(float) Width)*mapResolution;
                                                                                                                     else
                                                                                                                         actualResolution = (mapWidth / (float)ActualWidth) * mapResolution;
                                                                                                                     if (float.IsNaN(actualResolution) || float.IsInfinity(actualResolution))
                                                                                                                         actualResolution = 0;
                                                                                                                     else
                                                                                                                     {
                                                                                                                         Console.WriteLine("Actual rendered map resolution is: " + actualResolution);
                                                                                                                         MatchAspectRatio();
                                                                                                                     }
                                                                                                                     origin = new Point(i.info.origin.position.x, i.info.origin.position.y);
                                                                                                                     Size size = new Size(i.info.width, i.info.height);
                                                                                                                     byte[] data = createRGBA(i.data);
                                                                                                                     mGenericImage.UpdateImage(data, size, false);
                                                                                                                     data = null;
                                                                                                                 })));
            }
        }

        /// <summary>
        ///     Changes the Height of the control, so that it matches the aspect ratio of the map
        /// </summary>
        private void MatchAspectRatio()
        {
            double mapAspectRatio = mapWidth/(double) mapHeight;
            //Do nothing if map aspect ratio is close enough to control aspect ratio
            if (Math.Abs(Width/Height - mapAspectRatio) < 0.001)
                return;

            //Else, modify control Height to match map aspect ratio
            Height = Width/mapAspectRatio;
        }

        private byte[] createRGBA(sbyte[] map)
        {
            byte[] image = new byte[4*map.Length];
            int count = 0;
            foreach (sbyte j in map)
            {
                switch (j)
                {
                    case -1: ///Unkown occupancy, light gray
                        image[count] = 211;
                        image[count + 1] = 211;
                        image[count + 2] = 211;
                        image[count + 3] = 0xFF;
                        break;
                    case 100: //100% prob of occupancy, dark gray
                        image[count] = 105;
                        image[count + 1] = 105;
                        image[count + 2] = 105;
                        image[count + 3] = 0xFF;
                        break;
                    case 0: //0% prob of occupancy, White
                        image[count] = 255;
                        image[count + 1] = 255;
                        image[count + 2] = 255;
                        image[count + 3] = 0xFF;
                        break;
                    default: //Any other case. (red?)
                        image[count] = 255;
                        image[count + 1] = 0;
                        image[count + 2] = 0;
                        image[count + 3] = 0xFF;
                        break;
                }
                count += 4;
            }
            return image;
        }
    }
}