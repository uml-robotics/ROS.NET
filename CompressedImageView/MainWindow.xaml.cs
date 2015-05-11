#region Imports

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
using System;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using ROS_ImageWPF;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using System.Text;

#endregion


namespace CompressedImageView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			ROS.ROS_MASTER_URI = "http://notemind02:11311";
            ROS.Init(new string[0], "Image_Test");
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }

        private string[] topics = new string[6];

        private void flippydippy<T>(int i, T img) where T : UserControl
        {
            var c1 = img as ImageControl;
            var c2 = img as CompressedImageControl;
            if (c1 != null)
            {
                if (topics[i] == null)
                {
                    topics[i] = c1.Topic;
                    c1.guts.fps.Content = "PAUSED";
                    c1.shutdown();
                }
                else
                {
                    c1.Topic = topics[i];
                    c1.guts.fps.Content = "0";
                    topics[i] = null;
                }
            }
            else if (c2 != null)
            {
                if (topics[i] == null)
                {
                    topics[i] = c2.Topic;
                    c2.guts.fps.Content = "PAUSED";
                    c2.shutdown();
                }
                else
                {
                    c2.Topic = topics[i];
                    c2.guts.fps.Content = "0";
                    topics[i] = null;
                }
            }
            else 
                Console.WriteLine("TOO MANY ASSUMPTIONS!");
        }


        private void _1(object sender, RoutedEventArgs e)
        {
            flippydippy(0, TestImage1);
        }
        private void _2(object sender, RoutedEventArgs e)
        {
            flippydippy(1, TestImage2);
        }
        private void _3(object sender, RoutedEventArgs e)
        {
            flippydippy(2, TestImage3);
        }
        private void _4(object sender, RoutedEventArgs e)
        {
            flippydippy(3, TestImage4);
        }
        private void _5(object sender, RoutedEventArgs e)
        {
            flippydippy(4, TestImage5);
        }
        private void _6(object sender, RoutedEventArgs e)
        {
            flippydippy(5, TestImage6);
        }
    }
}
