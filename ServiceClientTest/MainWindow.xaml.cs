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

namespace ServiceClientTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NodeHandle nodeHandle;
        private string NODE_NAME = "ServiceClientTest";
        
        

        public MainWindow()
        {
            InitializeComponent();
        }
         private void Window_Loaded(object sender, RoutedEventArgs e) 
         {
            ROS.Init(new string[0], NODE_NAME+DateTime.Now.Ticks);

            nodeHandle = new NodeHandle();

            new Thread(new ThreadStart(() =>
                {
                    Random r = new Random();
                    while (ROS.ok)
                    {
                        TwoInts.Request req = new TwoInts.Request() { a = r.Next(100), b = r.Next(100) };
                        TwoInts.Response resp = new TwoInts.Response();
                        DateTime before = DateTime.Now;
                        bool res = nodeHandle.serviceClient<TwoInts.Request, TwoInts.Response>("/add_two_ints").call(req, ref resp);
                        TimeSpan dif = DateTime.Now.Subtract(before);
                            Dispatcher.Invoke(new Action(() =>
                                {
                                    string str = "";
                                    if (res)
                                        str = "" + req.a + " + " + req.b + " = " + resp.sum + "\n";
                                    else
                                        str = "call failed after\n";
                                    str += Math.Round(dif.TotalMilliseconds,2) + " ms";
                                    math.Content = str;
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
