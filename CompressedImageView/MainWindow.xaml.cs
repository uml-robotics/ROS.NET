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
            ROS.ROS_MASTER_URI = "http://robot-lab8:11311";
            ROS.Init(new string[0], "Image_Test");
            DispatcherTimer testies = new DispatcherTimer() { Interval = new TimeSpan(0,0,0,1) };
            testies.Tick += new EventHandler((o,e3)=>{
                gm.Vector3 trans = new gm.Vector3();
                gm.Quaternion rot = new gm.Quaternion();
                emTransform wheeee = tf_node.instance.transformFrame("/base_link", "/camera_link", out trans, out rot);
                if (wheeee != null && wheeee.translation != null)
                {
                    Console.WriteLine("base ==> camera: rpy=" + ((180.0 / Math.PI) * wheeee.rotation.getRPY()));
                }
                gm.Vector3 trans2 = new gm.Vector3();
                gm.Quaternion rot2 = new gm.Quaternion();
                emTransform wheeee2 = tf_node.instance.transformFrame("/camera_link", "/base_link", out trans2, out rot2);
                if (wheeee2 != null && wheeee2.translation != null)
                {
                    Console.WriteLine("camera ==> base: rpy=" + ((180.0 / Math.PI) * wheeee2.rotation.getRPY()));
                }
            });
            testies.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}
