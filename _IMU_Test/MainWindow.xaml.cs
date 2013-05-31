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
using System.Diagnostics;
using System;
using System.IO;
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
using Messages.rock_publisher;
using System.IO;
using am = Messages.sample_acquisition;

// for threading
using System.Windows.Threading;

// for controller; don't forget to include Microsoft.Xna.Framework in References
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// for timer
using System.Timers;

#endregion


namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        // initialize stuff for MainWindow
        public MainWindow()
        {
            InitializeComponent();
        }

        NodeHandle nh;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                // ROS stuff
                ROS.ROS_MASTER_URI = "http://10.0.3.5:11311";
                ROS.Init(new string[0], "The_IMU_Tester_" + System.Environment.MachineName.Replace("-", "__"));
                nh = new NodeHandle();

                new Thread(() =>
                {
                    while (!ROS.shutting_down)
                    {
                        ROS.spinOnce(ROS.GlobalNodeHandle);
                        Thread.Sleep(10);
                    }
                }).Start();
            }).Start();
        }

        // close ros when application closes
        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}