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


namespace RosoutDebug
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
            ROS.ROS_MASTER_URI = "http://10.0.3.88:11311";
            ROS.Init(new string[0], "Image_Test");


            NodeHandle node = new NodeHandle();

            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    Subscriber<Messages.rosgraph_msgs.Log> info = node.subscribe<Messages.rosgraph_msgs.Log>("/rosout_agg", 1000, callback);
                    ROS.spin();
                    Thread.Sleep(10);
                }
            }).Start();

        }

        private void callback(Messages.rosgraph_msgs.Log msg)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {
                //scroller.ScrollToBottom();
                textcolm0.Text += FromUnixTime(msg.header.stamp.data.sec) + "\n";
                textcolm1.Text += ConvertVerbosityLevel(msg.level) + "\n";
                textcolm2.Text += msg.name.data + "\n";
                textcolm3.Text += msg.msg.data + "\n";
                textcolm4.Text += msg.file.data + "\n";
                textcolm5.Text += msg.function.data + "\n";
            }));

        }

        
        private string ConvertVerbosityLevel( int level ) {

            if (level == 1)
                return "DEBUG";
            else if (level == 2)
                return "INFO";
            else if (level == 4)
                return "WARN";
            else if (level == 8)
                return "ERROR";
            else if (level == 16)
                return "FATAL";
            else return level.ToString();

        }
        
        public DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}
