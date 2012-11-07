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


namespace WpfApplication1
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
            ROS.ROS_MASTER_URI = "http://10.0.2.43:11311";
            ROS.ROS_HOSTNAME = "10.0.2.47";
            ROS.Init(new string[0], "Image_Test");
            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(ROS.GlobalNodeHandle);
                    Thread.Sleep(10);
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
