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
using System.Windows.Media.Animation;
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
using am = Messages.arm_status_msgs;


namespace armgauge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
    
        //private int panvalue;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            ROS.ROS_MASTER_URI = "http://10.0.3.88:11311";
            //ROS.ROS_MASTER_URI = "http://10.0.3.37:11311";
            ROS.ROS_HOSTNAME = "10.0.3.141";
            ROS.Init(new string[0], "Arm_Gauge");

            NodeHandle node = new NodeHandle();
           
            //panvalue = -90;

            new Thread(() =>
            {
            while (!ROS.shutting_down)
                {
                    Subscriber<am.ArmMovement> sub = node.subscribe<am.ArmMovement>("/arm/status", 1000, callback);
                    ROS.spin();
                    Thread.Sleep(1);
                }
            }).Start();

        }

        private void callback(am.ArmMovement msg)
        {

             Dispatcher.BeginInvoke(new Action(() =>
            {

                double tilt = msg.tilt_motor_position;
                double pan = msg.pan_motor_position;

                PanAnim.To = (pan * -90 + 180);
                TiltAnim.To = (tilt * -50);
                PanStory.Begin();
                TiltStory.Begin();

            }));

        }

    }
}


/*
               Rectangle pan = new Rectangle();
               pan.Width = 50;
               pan.Height = 5;
               pan.Fill = Brushes.Red;
               Grid.SetColumn(pan, 0);
               grid1.Children.Add(pan);
               pan.RenderTransform = new RotateTransform(panvalue, 2, 2);
                */