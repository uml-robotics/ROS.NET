// File: CompressedImageControl.xaml.cs
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
using sm = Messages.sensor_msgs;

#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///     Interaction logic for CompressedImageControl.xaml
    /// </summary>
    public partial class CompressedImageControl : UserControl, iROSImage
    {
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
                                                                       target.DrawImage();
                                                                   }
                                                               }
                                                               catch (Exception e)
                                                               {
                                                                   Console.WriteLine(e);
                                                               }
                                                           }));

        private NodeHandle imagehandle;
        private Subscriber<sm.CompressedImage> imgSub;
        private Thread waitingThread;

        public CompressedImageControl()
        {
            InitializeComponent();
        }

        private string __topic = null;

        /// <summary>
        ///     Gets/Sets Image provider topic and starts subscription
        /// </summary>
        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, (__topic = value)); }
        }

        public GenericImage getGenericImage()
        {
            return mGenericImage;
        }

        public bool IsSubscribed()
        {
            return imgSub != null;
        }

        public void Resubscribe()
        {
            Desubscribe();
            imgSub = imagehandle.subscribe<sm.CompressedImage>(Topic, 1, updateImage);
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

        private void DrawImage()
        {
            lock (this)
            {
                if (!ROS.isStarted())
                {
                    if (waitingThread == null)
                    {
                        waitingThread = new Thread(() => waitThenSubscribe());
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
            SubscribeToImage(Topic);
        }

        private void waitThenSubscribe()
        {
            while (true)
            {
                Thread.Sleep(100);
                lock(this)
                    if (ROS.shutting_down || ROS.isStarted())
                        break;
            }
            lock (this)
                if (ROS.shutting_down)
                    return;
            SubscribeToImage(__topic);
        }

        private void SubscribeToImage(string topic)
        {
            lock (this)
            {
                if (imagehandle == null)
                    imagehandle = new NodeHandle();
                if (imgSub != null && imgSub.topic != topic)
                {
                    imgSub.shutdown();
                    imgSub = null;
                }
                if (imgSub != null)
                    return;
                Console.WriteLine("Subscribing to image at:= " + topic);
                imgSub = imagehandle.subscribe<sm.CompressedImage>(topic, 1, updateImage);
            }
        }

        private void updateImage(sm.CompressedImage img)
        {
            Dispatcher.Invoke(new Action(() => mGenericImage.UpdateImage(img.data)));
        }
    }
}