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
using System.Threading;

using Messages;
using Messages.custom_msgs;
using Messages.roscpp_tutorials;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
//using d = System.Drawing;
using cm = Messages.custom_msgs;
using tf = Messages.tf;

namespace ServiceServerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NodeHandle nodeHandle;
        private string NODE_NAME = "ServiceServerTest";

        private ServiceServer server;

        private bool addition(TwoInts.Request req, ref TwoInts.Response resp)
        {
            resp.sum = req.a + req.b;
            long sum = resp.sum;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                math.Content = "" + req.a + " + " + req.b + " = ??\n" + sum;
            }));
            return true;
        }
         
        public MainWindow()
        {
            InitializeComponent();
        }
         private void Window_Loaded(object sender, RoutedEventArgs e) 
         {
            ROS.Init(new string[0], NODE_NAME);

            nodeHandle = new NodeHandle();

            server = nodeHandle.advertiseService<TwoInts.Request, TwoInts.Response>("/add_two_ints", addition);
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}
