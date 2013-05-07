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
using System.Collections.ObjectModel;

namespace RosoutDebugUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class RosoutDebug : UserControl
    {

        ObservableCollection<rosoutString> rosoutdata;

        public RosoutDebug()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return;

            ROS.ROS_MASTER_URI = "http://10.0.3.88:11311";
            ROS.Init(new string[0], "Image_Test");

            rosoutdata = new ObservableCollection<rosoutString>();
            dataGrid1.ItemsSource = rosoutdata;

            NodeHandle node = new NodeHandle();

            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    node.subscribe<Messages.rosgraph_msgs.Log>("/rosout_agg", 1000, callback);
                    ROS.spin();
                    Thread.Sleep(10);
                }
            }).Start();

        }

        //callback holla back
        private void callback(Messages.rosgraph_msgs.Log msg)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {

                string timestamp = DateTime.Now.ToShortTimeString() + "\n";
                string level= ConvertVerbosityLevel(msg.level) + "\n";
                string msgdata = msg.msg.data + "\n";
                string msgname = msg.name.data + "\n";

                //if (!(msgname == "/uirepublisher\n"))

                //slower than hell itself.  Should have used add().  Will regret it in the morning.
                rosoutdata.Insert( 0, new rosoutString(timestamp, level, msgdata, msgname) );
                cleanList();

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

        //prevents the list of growing too large. The hardcoded limit is set to 100 elements for the rosout display.
        public void cleanList()
        {

            if (rosoutdata.Count > 200)
                rosoutdata.RemoveAt(rosoutdata.Count - 1);

        }
    }

    //used for datagrid
    public class rosoutString
    {
        public string timestamp { get; set; }
        public string level { get; set; }
        public string msgdata { get; set; }
        public string msgname { get; set; }

        public rosoutString(string stamp, string level, string data, string name)
        {

            timestamp = stamp;
            this.level = level;
            msgdata = data;
            msgname = name;

        }
    }
}