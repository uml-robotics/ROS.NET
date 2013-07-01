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
using System.Threading;

using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
//using d = System.Drawing;
using cm = Messages.custom_msgs;
using tf = Messages.tf;

namespace ServiceTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NodeHandle nodeHandle;
        Publisher<Messages.sensor_msgs.CompressedImage> fuckYouNoob;
        private string NODE_NAME = "ServiceTest";

        public MainWindow()
        {
            InitializeComponent();
        }
         private void Window_Loaded(object sender, RoutedEventArgs e) 
         {
            ROS.ROS_MASTER_URI = "http://10.0.2.206:11311";
            ROS.ROS_HOSTNAME = "10.0.2.82";
            ROS.Init(new string[0], NODE_NAME);

            nodeHandle = new NodeHandle();
            
            fuckYouNoob = nodeHandle.advertise<Messages.sensor_msgs.CompressedImage>("/testing", 1);
            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    Messages.sensor_msgs.CompressedImage pow = new sm.CompressedImage();

                    fuckYouNoob.publish(pow);
                    Thread.Sleep(100);
                }
            }).Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}
