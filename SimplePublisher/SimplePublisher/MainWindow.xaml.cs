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

namespace SimplePublisher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Publisher<Messages.std_msgs.String> pub;
        NodeHandle nh;
        public MainWindow()
        {
            InitializeComponent();

            ROS.ROS_MASTER_URI = "http://10.0.2.226:11311";
            ROS.ROS_HOSTNAME = "10.0.2.226";
            ROS.Init(new string[0], "simplePublisher");
            nh = new NodeHandle();

            pub = nh.advertise<Messages.std_msgs.String>("/my_topic", 1000, true);

            
            new Thread(() =>
            {
                int i = 0;
                while (ROS.ok)
                {
                    Messages.std_msgs.String msg = new Messages.std_msgs.String();
                    msg.data = "Hello: " + i++;
                    pub.publish(msg);
                    Thread.Sleep(100);
                    Dispatcher.Invoke(new Action(() =>
                               {
                                   l.Content = "Sending: " + msg.data;
                               }));
                }
            }).Start();
            
        }
    }
}
