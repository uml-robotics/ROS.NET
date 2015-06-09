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

        public MainWindow()
        {
            InitializeComponent();
			ROS.ROS_MASTER_URI = "http://notemind02:11311";
            ROS.Init(new string[0], "wpf_listener");
            nh = new NodeHandle();

            sub = nh.subscribe<Messages.std_msgs.String>("/chatter", 10, subCallback);
        }

        public void subCallback(Messages.std_msgs.String msg)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                l.Content = "Receieved:\n" + msg.data;
            }), new TimeSpan(0,0,1));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            ROS.shutdown();
            ROS.waitForShutdown();
            base.OnClosing(e);
        }
    }
}
