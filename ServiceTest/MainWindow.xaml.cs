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

namespace ServiceTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NodeHandle nodeHandle;
        private string NODE_NAME = "ServiceTest";
        //private ServiceServer<Messages.roscpp_tutorials.TwoInts, Messages.roscpp_tutorials.TwoInts.Request, Messages.roscpp_tutorials.TwoInts.Response> server;
        private ServiceClient<Messages.roscpp_tutorials.TwoInts.Request, Messages.roscpp_tutorials.TwoInts.Response> client;

        private bool addition(Messages.roscpp_tutorials.TwoInts.Request req, ref Messages.roscpp_tutorials.TwoInts.Response resp)
        {
            resp.sum = req.a + req.b;
            return true;
        }

        public MainWindow()
        {
            InitializeComponent();
        }
         private void Window_Loaded(object sender, RoutedEventArgs e) 
         {
            ROS.ROS_MASTER_URI = "http://10.0.2.88:11311";
            ROS.ROS_HOSTNAME = "10.0.2.152";
            ROS.Init(new string[0], NODE_NAME);

            nodeHandle = new NodeHandle();

            //server = nodeHandle.advertiseService<Messages.roscpp_tutorials.TwoInts, Messages.roscpp_tutorials.TwoInts.Request, Messages.roscpp_tutorials.TwoInts.Response>("/add_two_ints", addition);
            client = nodeHandle.serviceClient<Messages.roscpp_tutorials.TwoInts.Request, Messages.roscpp_tutorials.TwoInts.Response>("/add_two_ints");

            new Thread(new ThreadStart(() =>
                {
                    Random r = new Random();
                    while (!ROS.shutting_down)
                    {
                        TwoInts.Request req = new TwoInts.Request() { a = r.Next(100), b = r.Next(100) };
                        TwoInts.Response resp = new TwoInts.Response();
                        if (client.call(req, ref resp))
                            Dispatcher.Invoke(new Action(() =>
                                {
                                    math.Content = "" + req.a + " + " + req.b + " = " + resp.sum;
                                }));
                        Thread.Sleep(500);
                    }
                })).Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}
