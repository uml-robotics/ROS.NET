using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ros_CSharp;
using System.Threading;
using sm = Messages.sensor_msgs;

namespace ROS_ImageWPF
{
    /// <summary>
    /// Interaction logic for CompressedImageControl.xaml
    /// </summary>
    public partial class CompressedImageControl : UserControl
    {
        private NodeHandle imagehandle;
        private Subscriber<sm.CompressedImage> imgSub;
        private Thread waitingThread;
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
                            target.DrawImage();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
        }));

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
                imgSub.shutdown();
                imgSub = null;
            }
            if (imgSub != null)
                return;
            Console.WriteLine("Subscribing to image at:= " + topic);
            imgSub = imagehandle.subscribe<sm.CompressedImage>(topic, 1, updateImage);
        }

        private void updateImage(sm.CompressedImage img)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                mGenericImage.UpdateImage(img.data);
            }));
        }

        /// <summary>
        /// Gets/Sets Image provider topic and starts subscription
        /// </summary>
        public string Topic
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }

        public void Resubscribe()
        {
            if (imgSub != null)
            {
                Desubscribe();
            }
            imgSub = imagehandle.subscribe<sm.CompressedImage>(Topic, 1, updateImage);
        }

        /// <summary>
        /// Stops Subscription
        /// </summary>
        public void Desubscribe()
        {
            imgSub.shutdown();
            imgSub = null;
        }

        public CompressedImageControl()
        {
            InitializeComponent();
        }
    }
}
