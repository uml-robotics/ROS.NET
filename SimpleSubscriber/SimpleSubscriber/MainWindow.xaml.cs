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
using XmlRpc_Wrapper;
using Messages;
using System.Threading;

namespace SimpleSubscriber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Subscriber<Messages.std_msgs.String> sub;
        NodeHandle nh;
        DateTime before;
        public MainWindow()
        {
            before = DateTime.Now;
            InitializeComponent();

            ROS.ROS_MASTER_URI = "http://10.0.2.226:11311";
            ROS.ROS_HOSTNAME = "10.0.2.226";
            ROS.Init(new string[0], "simpleSubscriber");
            nh = new NodeHandle();

            sub = nh.subscribe<Messages.std_msgs.String>("/my_topic", 10, subCallback);
        }

        public void subCallback(Messages.std_msgs.String msg)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                TimeSpan dif = DateTime.Now.Subtract(before);
                l.Content = "Receieved: " + msg.data + "\n" + Math.Round(dif.TotalMilliseconds, 2) + " ms"; ;
            }));

            before = DateTime.Now;
        }
    }
}
