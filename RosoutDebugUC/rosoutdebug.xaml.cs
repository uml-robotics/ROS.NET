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

namespace RosoutDebugUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            
            NodeHandle node = new NodeHandle();

            new Thread(() =>
            {
                Subscriber<Messages.rosgraph_msgs.Log> info = node.subscribe<Messages.rosgraph_msgs.Log>("/rosout_agg", 1000, callback);
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce();
                    Thread.Sleep(10);
                }
            }).Start();

        }

        private void callback(Messages.rosgraph_msgs.Log msg)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {
                scroller.ScrollToBottom();
                textcolm0.Text += DateTime.Now.ToShortTimeString() + "\n";
                textcolm1.Text += ConvertVerbosityLevel(msg.level) + "\n";
                textcolm2.Text += msg.msg.data + "\n";
                textcolm3.Text += msg.name.data + "\n";
                //textcolm4.Text += msg.file.data + "\n";
                //textcolm5.Text += msg.function.data + "\n";
            }));

        }


        private string ConvertVerbosityLevel(int level)
        {

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
        
        /*
        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }*/
    }
}