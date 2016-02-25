// File: ImageControl.xaml.cs
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
    public partial class ImageControl : UserControl, iROSImage
    {
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
                                                                       target.DrawImage();
                                                                   }
                                                               }
                                                               catch (Exception e)
                                                               {
                                                                   Console.WriteLine(e);
                                                               }
                                                           }));

        private NodeHandle imagehandle;
        private Subscriber<sm.Image> imgSub;
        private Thread waitingThread;

        public ImageControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Gets/Sets Image provider topic and starts subscription
        /// </summary>
        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }

        public void Resubscribe()
        {
            Desubscribe();
            imgSub = imagehandle.subscribe<sm.Image>(Topic, 1, updateImage);
        }

        public GenericImage getGenericImage()
        {
            return mGenericImage;
        }

        public bool IsSubscribed()
        {
            return imgSub != null;
        }

        /// <summary>
        ///     Stops Subscription
        /// </summary>
        public void Desubscribe()
        {
            if (imgSub != null)
            {
                imgSub.shutdown();
                imgSub = null;
            }
        }

        public event FPSEvent fpsevent;

        private void DrawImage()
        {
            if (!ROS.isStarted())
            {
                if (waitingThread == null)
                {
                    string topicString = Topic;
                    waitingThread = new Thread(() => waitThenSubscribe(topicString));
                }
                if (!waitingThread.IsAlive)
                {
                    waitingThread.Start();
                }
            }
            else
                SubscribeToImage(Topic);
        }

        private void waitThenSubscribe(string topic)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);
            SubscribeToImage(topic);
        }

        private void SubscribeToImage(string topic)
        {
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (imgSub != null && imgSub.topic != topic)
            {
                Desubscribe();
            }
            if (imgSub != null)
                return;
            Console.WriteLine("Subscribing to image at:= " + topic);
            imgSub = imagehandle.subscribe<sm.Image>(topic, 1, updateImage);
        }

        private void updateImage(sm.Image img)
        {
            Dispatcher.Invoke(new Action(() => mGenericImage.UpdateImage(img.data, new Size((int) img.width, (int) img.height), false, img.encoding)));
        }
    }
}