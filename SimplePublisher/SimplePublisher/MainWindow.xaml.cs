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
        Publisher<Messages.tf.tfMessage> pub;
        NodeHandle nh;
        public MainWindow()
        {
            InitializeComponent();

            ROS.Init(new string[0], "simplePublisher");
            nh = new NodeHandle();

            pub = nh.advertise<Messages.tf.tfMessage>("/tf_test", 1000, true);

            new Thread(() =>
            {
                int i = 0;
                while (ROS.ok)
                {
                    Messages.tf.tfMessage msg = new Messages.tf.tfMessage();
                    msg.transforms = new Messages.geometry_msgs.TransformStamped[1];
                    msg.transforms[0] = new Messages.geometry_msgs.TransformStamped();
                    msg.transforms[0].header.seq = (uint)i++;
                    pub.publish(msg);
                    Thread.Sleep(100);
                    Dispatcher.Invoke(new Action(() =>
                               {
                                   l.Content = "Sending: " + msg.transforms[0].header.seq;
                               }));
                }
            }).Start();
            
        }
    }
}
